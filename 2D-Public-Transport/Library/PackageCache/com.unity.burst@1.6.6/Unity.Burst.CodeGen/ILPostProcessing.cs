using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace zzzUnity.Burst.CodeGen
{
    internal delegate void LogDelegate(string message);
    internal delegate void ErrorDiagnosticDelegate(MethodDefinition method, Instruction instruction, string message);

    /// <summary>
    /// Main class for post processing assemblies. The post processing is currently performing:
    /// - Replace C# call from C# to Burst functions with attributes [BurstCompile] to a call to the compiled Burst function
    ///   In both editor and standalone scenarios. For DOTS Runtime, this is done differently at BclApp level by patching
    ///   DllImport.
    /// </summary>
    internal class ILPostProcessing
    {
        private AssemblyDefinition _burstAssembly;
        private TypeDefinition _burstCompilerTypeDefinition;
        private MethodReference _burstCompilerIsEnabledMethodDefinition;
        private MethodReference _burstCompilerCompileILPPMethod;
        private MethodReference _burstCompilerGetILPPMethodFunctionPointer;
        private MethodReference _burstDiscardAttributeConstructor;
        private MethodReference _burstCompilerCompileUnsafeStaticMethodReinitialiseAttributeCtor;
        private TypeSystem _typeSystem;
        private TypeReference _systemType;
        private TypeReference _systemDelegateType;
        private TypeReference _systemASyncCallbackType;
        private TypeReference _systemIASyncResultType;
        private AssemblyDefinition _assemblyDefinition;
        private bool _modified;
        private readonly List<MethodDefinition> _directCallInitializeMethods = new List<MethodDefinition>();

        private const string PostfixBurstDirectCall = "$BurstDirectCall";
        private const string PostfixBurstDelegate = "$PostfixBurstDelegate";
        private const string PostfixManaged = "$BurstManaged";
        private const string GetFunctionPointerName = "GetFunctionPointer";
        private const string GetFunctionPointerDiscardName = "GetFunctionPointerDiscard";
        private const string InvokeName = "Invoke";

        public ILPostProcessing(AssemblyLoader loader, bool isForEditor, ErrorDiagnosticDelegate error, LogDelegate log = null, int logLevel = 0)
        {
            Loader = loader;
            IsForEditor = isForEditor;
        }

        /// <summary>
        /// Not used, but we cannot remove the public API.
        /// </summary>
        /// <param name="typeDefinition">Declaring type of a direct call</param>
        public static MethodReference RecoverManagedMethodFromDirectCall(TypeDefinition typeDefinition) => null;

        public bool IsForEditor { get; private set; }

        public AssemblyLoader Loader { get; }

        /// <summary>
        /// Checks the specified method is a direct call.
        /// </summary>
        /// <param name="method">The method being called</param>
        public static bool IsDirectCall(MethodDefinition method)
        {
            // Method can be null so we early exit without a NullReferenceException
            return method != null && method.IsStatic && method.Name == InvokeName && method.DeclaringType.Name.EndsWith(ILPostProcessing.PostfixBurstDirectCall);
        }

        public unsafe bool Run(IntPtr peData, int peSize, IntPtr pdbData, int pdbSize, out AssemblyDefinition assemblyDefinition)
        {
            if (peData == IntPtr.Zero) throw new ArgumentNullException(nameof(peData));
            if (peSize <= 0) throw new ArgumentOutOfRangeException(nameof(peSize));
            if (pdbData != IntPtr.Zero && pdbSize <= 0) throw new ArgumentOutOfRangeException(nameof(pdbSize));

            var peStream = new UnmanagedMemoryStream((byte*)peData, peSize);
            Stream pdbStream = null;

            if (pdbData != IntPtr.Zero)
            {
                pdbStream = new UnmanagedMemoryStream((byte*)pdbData, pdbSize);
            }

            assemblyDefinition = Loader.LoadFromStream(peStream, pdbStream);
            return Run(assemblyDefinition);
        }

        public bool Run(AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;
            _typeSystem = assemblyDefinition.MainModule.TypeSystem;

            _modified = false;
            var types = assemblyDefinition.MainModule.GetTypes().ToArray();
            foreach (var type in types)
            {
                ProcessType(type);
            }

            // If we processed any direct-calls, then generate a single [RuntimeInitializeOnLoadMethod] method
            // for the whole assembly, which will initialize each individual direct-call class.
            if (_directCallInitializeMethods.Count > 0)
            {
                GenerateInitializeOnLoadMethod();
            }

            return _modified;
        }

        private void GenerateInitializeOnLoadMethod()
        {
            // [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
            // [UnityEditor.InitializeOnLoadMethod] // When its an editor assembly
            // private static void Initialize()
            // {
            //     DirectCallA.Initialize();
            //     DirectCallB.Initialize();
            // }
            const string initializeOnLoadClassName = "$BurstDirectCallInitializer";
            var initializeOnLoadClass = _assemblyDefinition.MainModule.Types.FirstOrDefault(x => x.Name == initializeOnLoadClassName);
            if (initializeOnLoadClass != null)
            {
                // If there's already a class with this name, remove it,
                // This would mean that we're postprocessing an already-postprocessed assembly;
                // I don't think that ever happens, but no sense in breaking if it does.
                _assemblyDefinition.MainModule.Types.Remove(initializeOnLoadClass);
            }
            initializeOnLoadClass = new TypeDefinition(
                "",
                initializeOnLoadClassName,
                TypeAttributes.NotPublic |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Abstract |
                TypeAttributes.Sealed |
                TypeAttributes.BeforeFieldInit)
            {
                BaseType = _typeSystem.Object
            };
            _assemblyDefinition.MainModule.Types.Add(initializeOnLoadClass);
            var initializeOnLoadMethod = new MethodDefinition("Initialize", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.Void)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = initializeOnLoadClass
            };

            var processor = initializeOnLoadMethod.Body.GetILProcessor();
            foreach (var initializeMethod in _directCallInitializeMethods)
            {
                processor.Emit(OpCodes.Call, initializeMethod);
            }
            processor.Emit(OpCodes.Ret);
            initializeOnLoadClass.Methods.Add(FixDebugInformation(initializeOnLoadMethod));

            var attribute = new CustomAttribute(_unityEngineInitializeOnLoadAttributeCtor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(_unityEngineRuntimeInitializeLoadType, _unityEngineRuntimeInitializeLoadAfterAssemblies.Constant));
            initializeOnLoadMethod.CustomAttributes.Add(attribute);

            if (IsForEditor)
            {
                // Need to ensure the editor tag for initialize on load is present, otherwise edit mode tests will not call Initialize
                attribute = new CustomAttribute(_unityEditorInitilizeOnLoadAttributeCtor);
                initializeOnLoadMethod.CustomAttributes.Add(attribute);
            }
        }

        private void ProcessType(TypeDefinition type)
        {
            if (!type.HasGenericParameters && TryGetBurstCompilerAttribute(type, out _))
            {
                // Make a copy because we are going to modify it
                var methodCount = type.Methods.Count;
                for (var j = 0; j < methodCount; j++)
                {
                    var method = type.Methods[j];
                    if (!method.IsStatic || method.HasGenericParameters || !TryGetBurstCompilerAttribute(method, out var methodBurstCompileAttribute)) continue;

                    bool isDirectCallDisabled = false;
                    bool foundProperty = false;
                    if (methodBurstCompileAttribute.HasProperties)
                    {
                        foreach (var property in methodBurstCompileAttribute.Properties)
                        {
                            if (property.Name == "DisableDirectCall")
                            {
                                isDirectCallDisabled = (bool)property.Argument.Value;
                                foundProperty = true;
                                break;
                            }
                        }
                    }

                    // If the method doesn't have a direct call specified, try the assembly level, do one last check for any assembly level [BurstCompile] instead.
                    if (foundProperty == false && TryGetBurstCompilerAttribute(method.Module.Assembly, out var assemblyBurstCompileAttribute))
                    {
                        if (assemblyBurstCompileAttribute.HasProperties)
                        {
                            foreach (var property in assemblyBurstCompileAttribute.Properties)
                            {
                                if (property.Name == "DisableDirectCall")
                                {
                                    isDirectCallDisabled = (bool)property.Argument.Value;
                                    break;
                                }
                            }
                        }
                    }

#if !UNITY_DOTSPLAYER       // Direct call is not Supported for dots runtime via this pre-processor, its handled elsewhere, this code assumes a Unity Editor based burst
                    if (!isDirectCallDisabled)
                    {
                        if (_burstAssembly == null)
                        {
                            var resolved = methodBurstCompileAttribute.Constructor.DeclaringType.Resolve();
                            InitializeBurstAssembly(resolved.Module.Assembly);
                        }

                        ProcessMethodForDirectCall(method);
                        _modified = true;
                    }
#endif
                }
            }
        }

        private TypeDefinition InjectDelegate(TypeDefinition declaringType, string originalName, MethodDefinition managed, string uniqueSuffix)
        {
            var injectedDelegateType = new TypeDefinition(declaringType.Namespace, $"{originalName}{uniqueSuffix}{PostfixBurstDelegate}",
                TypeAttributes.NestedPublic |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Sealed
            )
            {
                DeclaringType = declaringType,
                BaseType = _systemDelegateType
            };

            declaringType.NestedTypes.Add(injectedDelegateType);

            {
                var constructor = new MethodDefinition(".ctor",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                    _typeSystem.Void)
                {
                    HasThis = true,
                    IsManaged = true,
                    IsRuntime = true,
                    DeclaringType = injectedDelegateType
                };

                constructor.Parameters.Add(new ParameterDefinition(_typeSystem.Object));
                constructor.Parameters.Add(new ParameterDefinition(_typeSystem.IntPtr));
                injectedDelegateType.Methods.Add(constructor);
            }

            {
                var invoke = new MethodDefinition("Invoke",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot |
                    MethodAttributes.Virtual,
                    managed.ReturnType)
                {
                    HasThis = true,
                    IsManaged = true,
                    IsRuntime = true,
                    DeclaringType = injectedDelegateType
                };

                foreach (var parameter in managed.Parameters)
                {
                    invoke.Parameters.Add(parameter);
                }

                injectedDelegateType.Methods.Add(invoke);
            }

            {
                var beginInvoke = new MethodDefinition("BeginInvoke",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot |
                    MethodAttributes.Virtual,
                    _systemIASyncResultType)
                {
                    HasThis = true,
                    IsManaged = true,
                    IsRuntime = true,
                    DeclaringType = injectedDelegateType
                };

                foreach (var parameter in managed.Parameters)
                {
                    beginInvoke.Parameters.Add(parameter);
                }

                beginInvoke.Parameters.Add(new ParameterDefinition(_systemASyncCallbackType));
                beginInvoke.Parameters.Add(new ParameterDefinition(_typeSystem.Object));

                injectedDelegateType.Methods.Add(beginInvoke);
            }

            {
                var endInvoke = new MethodDefinition("EndInvoke",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot |
                    MethodAttributes.Virtual,
                    managed.ReturnType)
                {
                    HasThis = true,
                    IsManaged = true,
                    IsRuntime = true,
                    DeclaringType = injectedDelegateType
                };

                endInvoke.Parameters.Add(new ParameterDefinition(_systemIASyncResultType));

                injectedDelegateType.Methods.Add(endInvoke);
            }

            return injectedDelegateType;
        }

        private MethodDefinition CreateGetFunctionPointerDiscardMethod(TypeDefinition cls, FieldDefinition pointerField, FieldDefinition deferredCompilationField, MethodDefinition burstCompileMethod)
        {
            // Create GetFunctionPointer method:
            //
            // [BurstDiscard]
            // public static void GetFunctionPointerDiscard(ref IntPtr ptr) {
            //   if (Pointer == null) {
            //     Pointer = BurstCompiler.GetILPPMethodFunctionPointer(DeferredCompilation);
            //   }
            //
            //   ptr = Pointer
            // }
            var getFunctionPointerDiscardMethod = new MethodDefinition(GetFunctionPointerDiscardName, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.Void)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls
            };

            getFunctionPointerDiscardMethod.Parameters.Add(new ParameterDefinition(new ByReferenceType(_typeSystem.IntPtr)));

            var processor = getFunctionPointerDiscardMethod.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldsfld, pointerField);
            var branchPosition = processor.Body.Instructions[processor.Body.Instructions.Count - 1];

            processor.Emit(OpCodes.Ldsfld, deferredCompilationField);
            processor.Emit(OpCodes.Call, _burstCompilerGetILPPMethodFunctionPointer);
            processor.Emit(OpCodes.Stsfld, pointerField);

            processor.Emit(OpCodes.Ldarg_0);
            processor.InsertAfter(branchPosition, Instruction.Create(OpCodes.Brtrue, processor.Body.Instructions[processor.Body.Instructions.Count - 1]));
            processor.Emit(OpCodes.Ldsfld, pointerField);
            processor.Emit(OpCodes.Stind_I);
            processor.Emit(OpCodes.Ret);

            cls.Methods.Add(FixDebugInformation(getFunctionPointerDiscardMethod));

            getFunctionPointerDiscardMethod.CustomAttributes.Add(new CustomAttribute(_burstDiscardAttributeConstructor));

            return getFunctionPointerDiscardMethod;
        }

        private MethodDefinition CreateGetFunctionPointerMethod(TypeDefinition cls, MethodDefinition getFunctionPointerDiscardMethod)
        {
            // Create GetFunctionPointer method:
            //
            // public static IntPtr GetFunctionPointer() {
            //   var ptr;
            //   GetFunctionPointerDiscard(ref ptr);
            //   return ptr;
            // }
            var getFunctionPointerMethod = new MethodDefinition(GetFunctionPointerName, MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.IntPtr)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls
            };

            getFunctionPointerMethod.Body.Variables.Add(new VariableDefinition(_typeSystem.IntPtr));
            getFunctionPointerMethod.Body.InitLocals = true;

            var processor = getFunctionPointerMethod.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Conv_I);
            processor.Emit(OpCodes.Stloc_0);
            processor.Emit(OpCodes.Ldloca_S, (byte)0);
            processor.Emit(OpCodes.Call, getFunctionPointerDiscardMethod);
            processor.Emit(OpCodes.Ldloc_0);

            processor.Emit(OpCodes.Ret);

            cls.Methods.Add(FixDebugInformation(getFunctionPointerMethod));

            return getFunctionPointerMethod;
        }

        private void ProcessMethodForDirectCall(MethodDefinition burstCompileMethod)
        {
            var declaringType = burstCompileMethod.DeclaringType;

            var uniqueSuffix = $"_{burstCompileMethod.MetadataToken.RID:X8}";

            var injectedDelegate = InjectDelegate(declaringType, burstCompileMethod.Name, burstCompileMethod, uniqueSuffix);

            // Create a copy of the original method that will be the actual managed method
            // The original method is patched at the end of this method to call
            // the dispatcher that will go to the Burst implementation or the managed method (if in the editor and Burst is disabled)
            var managedFallbackMethod = new MethodDefinition($"{burstCompileMethod.Name}{PostfixManaged}", burstCompileMethod.Attributes, burstCompileMethod.ReturnType)
            {
                DeclaringType = declaringType,
                ImplAttributes = burstCompileMethod.ImplAttributes,
                MetadataToken = burstCompileMethod.MetadataToken,
            };

            declaringType.Methods.Add(managedFallbackMethod);

            foreach (var parameter in burstCompileMethod.Parameters)
            {
                managedFallbackMethod.Parameters.Add(parameter);
            }

            foreach (var customAttr in burstCompileMethod.CustomAttributes)
            {
                managedFallbackMethod.CustomAttributes.Add(customAttr);
            }

            // Remove the [BurstCompile] on the new managed function.
            if (TryGetBurstCompilerAttribute(managedFallbackMethod, out var managedBurstCompileAttribute))
            {
                managedFallbackMethod.CustomAttributes.Remove(managedBurstCompileAttribute);
            }

            // Copy the body from the original burst method to the managed fallback, we'll replace the burstCompileMethod body later.
            managedFallbackMethod.Body.InitLocals = burstCompileMethod.Body.InitLocals;
            managedFallbackMethod.Body.LocalVarToken = burstCompileMethod.Body.LocalVarToken;
            managedFallbackMethod.Body.MaxStackSize = burstCompileMethod.Body.MaxStackSize;
            managedFallbackMethod.Body.Scope = burstCompileMethod.Body.Scope;

            foreach (var variable in burstCompileMethod.Body.Variables)
            {
                managedFallbackMethod.Body.Variables.Add(variable);
            }

            foreach (var instruction in burstCompileMethod.Body.Instructions)
            {
                managedFallbackMethod.Body.Instructions.Add(instruction);
            }

            foreach (var exceptionHandler in burstCompileMethod.Body.ExceptionHandlers)
            {
                managedFallbackMethod.Body.ExceptionHandlers.Add(exceptionHandler);
            }

            managedFallbackMethod.ImplAttributes &= MethodImplAttributes.NoInlining;
            // 0x0100 is AggressiveInlining
            managedFallbackMethod.ImplAttributes |= (MethodImplAttributes)0x0100;

            // The method needs to be public because we query for it in the ILPP code.
            managedFallbackMethod.Attributes &= ~MethodAttributes.Private;
            managedFallbackMethod.Attributes |= MethodAttributes.Public;

            // private static class (Name_RID.$Postfix)
            var cls = new TypeDefinition(declaringType.Namespace, $"{burstCompileMethod.Name}{uniqueSuffix}{PostfixBurstDirectCall}",
                TypeAttributes.NestedAssembly |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Abstract |
                TypeAttributes.Sealed |
                TypeAttributes.BeforeFieldInit
            )
            {
                DeclaringType = declaringType,
                BaseType = _typeSystem.Object
            };

            declaringType.NestedTypes.Add(cls);

            // Create Field:
            //
            // private static IntPtr Pointer;
            var pointerField = new FieldDefinition("Pointer", FieldAttributes.Static | FieldAttributes.Private, _typeSystem.IntPtr)
            {
                DeclaringType = cls
            };
            cls.Fields.Add(pointerField);

            // Create Field:
            //
            // private static IntPtr DeferredCompilation;
            var deferredCompilationField = new FieldDefinition("DeferredCompilation", FieldAttributes.Static | FieldAttributes.Private, _typeSystem.IntPtr)
            {
                DeclaringType = cls
            };
            cls.Fields.Add(deferredCompilationField);

            var getFunctionPointerDiscardMethod = CreateGetFunctionPointerDiscardMethod(cls, pointerField, deferredCompilationField, burstCompileMethod);
            var getFunctionPointerMethod = CreateGetFunctionPointerMethod(cls, getFunctionPointerDiscardMethod);

            var asmAttribute = new CustomAttribute(_burstCompilerCompileUnsafeStaticMethodReinitialiseAttributeCtor);
            asmAttribute.ConstructorArguments.Add(new CustomAttributeArgument(_systemType, cls));
            _assemblyDefinition.CustomAttributes.Add(asmAttribute);

            // Create the static Constructor Method (called via .cctor and via reflection on burst compilation enable)
            // private static void Constructor() {
            //   deferredCompilation = CompileILPPMethod(burstCompileMethod, managedFallbackMethod, DelegateType);
            // }

            var constructor = new MethodDefinition("Constructor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.Void)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls
            };

            var processor = constructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldtoken, burstCompileMethod);
            processor.Emit(OpCodes.Ldtoken, managedFallbackMethod);
            processor.Emit(OpCodes.Ldtoken, injectedDelegate);
            processor.Emit(OpCodes.Call, _burstCompilerCompileILPPMethod);
            processor.Emit(OpCodes.Stsfld, deferredCompilationField);
            processor.Emit(OpCodes.Ret);

            cls.Methods.Add(FixDebugInformation(constructor));

            // Create an Initialize method
            // This will be called from the single [RuntimeInitializeOnLoadMethod]
            // method that we'll generate for this assembly.
            // Its only job is to cause the .cctor to run.
            //
            // public static void Initialize() { }
            var initializeMethod = new MethodDefinition("Initialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.Void)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls
            };

            processor = initializeMethod.Body.GetILProcessor();
            processor.Emit(OpCodes.Ret);
            cls.Methods.Add(FixDebugInformation(initializeMethod));

            var currentInitializer = initializeMethod;
            var currentDeclaringType = declaringType;

            // If our target method is hidden behind one or more nested private classes, then
            // create a method on the parent type that calls said method (for each private nested class)
            while (currentDeclaringType.DeclaringType != null)
            {
                var parentType = currentDeclaringType.DeclaringType;
                if (((currentDeclaringType.Attributes & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate))
                {
                    var redirectingInitializer = new MethodDefinition($"Initialize${declaringType.Name}_{cls.Name}",
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                        _typeSystem.Void)
                    {
                        ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                        DeclaringType = parentType
                    };
                    processor = redirectingInitializer.Body.GetILProcessor();
                    processor.Emit(OpCodes.Call, currentInitializer);
                    processor.Emit(OpCodes.Ret);
                    parentType.Methods.Add(redirectingInitializer);
                    currentInitializer = redirectingInitializer;
                }

                currentDeclaringType = parentType;
            }

            _directCallInitializeMethods.Add(currentInitializer);

            // Create the static constructor
            //
            // public static .cctor() {
            //   Constructor();
            // }
            var cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static, _typeSystem.Void)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls,
            };

            processor = cctor.Body.GetILProcessor();
            processor.Emit(OpCodes.Call, constructor);
            processor.Emit(OpCodes.Ret);

            cls.Methods.Add(FixDebugInformation(cctor));

            // Create the Invoke method based on the original method (same signature)
            //
            // public static XXX Invoke(...args) {
            //    if (BurstCompiler.IsEnabled)
            //    {
            //        var funcPtr = GetFunctionPointer();
            //        if (funcPtr != null) return funcPtr(...args);
            //    }
            //    return OriginalMethod(...args);
            // }
            var invokeAttributes = managedFallbackMethod.Attributes;
            invokeAttributes &= ~MethodAttributes.Private;
            invokeAttributes |= MethodAttributes.Public;
            var invoke = new MethodDefinition(InvokeName, invokeAttributes, burstCompileMethod.ReturnType)
            {
                ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed,
                DeclaringType = cls
            };

            var signature = new CallSite(burstCompileMethod.ReturnType)
            {
                CallingConvention = MethodCallingConvention.C
            };

            foreach (var parameter in burstCompileMethod.Parameters)
            {
                invoke.Parameters.Add(parameter);
                signature.Parameters.Add(parameter);
            }

            invoke.Body.Variables.Add(new VariableDefinition(_typeSystem.IntPtr));
            invoke.Body.InitLocals = true;

            processor = invoke.Body.GetILProcessor();
            processor.Emit(OpCodes.Call, _burstCompilerIsEnabledMethodDefinition);
            var branchPosition0 = processor.Body.Instructions[processor.Body.Instructions.Count - 1];

            processor.Emit(OpCodes.Call, getFunctionPointerMethod);
            processor.Emit(OpCodes.Stloc_0);
            processor.Emit(OpCodes.Ldloc_0);
            var branchPosition1 = processor.Body.Instructions[processor.Body.Instructions.Count - 1];

            EmitArguments(processor, invoke);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Calli, signature);
            processor.Emit(OpCodes.Ret);
            var previousRet = processor.Body.Instructions[processor.Body.Instructions.Count - 1];

            EmitArguments(processor, invoke);
            processor.Emit(OpCodes.Call, managedFallbackMethod);
            processor.Emit(OpCodes.Ret);

            // Insert the branch once we have emitted the instructions
            processor.InsertAfter(branchPosition0, Instruction.Create(OpCodes.Brfalse, previousRet.Next));
            processor.InsertAfter(branchPosition1, Instruction.Create(OpCodes.Brfalse, previousRet.Next));
            cls.Methods.Add(FixDebugInformation(invoke));

            // Final patching of the original method
            // public static XXX OriginalMethod(...args) {
            //      Name_RID.$Postfix.Invoke(...args);
            //      ret;
            // }
            burstCompileMethod.Body = new MethodBody(burstCompileMethod);
            processor = burstCompileMethod.Body.GetILProcessor();
            EmitArguments(processor, burstCompileMethod);
            processor.Emit(OpCodes.Call, invoke);
            processor.Emit(OpCodes.Ret);
            FixDebugInformation(burstCompileMethod);
        }

        private static MethodDefinition FixDebugInformation(MethodDefinition method)
        {
            method.DebugInformation.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.Last());
            return method;
        }

        private AssemblyDefinition GetAsmDefinitionFromFile(AssemblyLoader loader, string filename)
        {
            foreach (var folder in loader.GetSearchDirectories())
            {
                var path = Path.Combine(folder, filename);
                if (File.Exists(path))
                    return loader.LoadFromFile(path);
            }
            return null;
        }

        private MethodReference _unityEngineInitializeOnLoadAttributeCtor;
        private TypeReference _unityEngineRuntimeInitializeLoadType;
        private FieldDefinition _unityEngineRuntimeInitializeLoadAfterAssemblies;
        private MethodReference _unityEditorInitilizeOnLoadAttributeCtor;

        private void InitializeBurstAssembly(AssemblyDefinition burstAssembly)
        {
            _burstAssembly = burstAssembly;
            _burstCompilerTypeDefinition = burstAssembly.MainModule.GetType("Unity.Burst", "BurstCompiler");

            _burstCompilerIsEnabledMethodDefinition = _assemblyDefinition.MainModule.ImportReference(_burstCompilerTypeDefinition.Methods.FirstOrDefault(x => x.Name == "get_IsEnabled"));
            _burstCompilerCompileILPPMethod = _assemblyDefinition.MainModule.ImportReference(_burstCompilerTypeDefinition.Methods.FirstOrDefault(x => x.Name == "CompileILPPMethod"));
            _burstCompilerGetILPPMethodFunctionPointer = _assemblyDefinition.MainModule.ImportReference(_burstCompilerTypeDefinition.Methods.FirstOrDefault(x => x.Name == "GetILPPMethodFunctionPointer"));

            var reinitializeAttribute = _burstCompilerTypeDefinition.NestedTypes.FirstOrDefault(x => x.Name == "StaticTypeReinitAttribute");
            _burstCompilerCompileUnsafeStaticMethodReinitialiseAttributeCtor = _assemblyDefinition.MainModule.ImportReference(reinitializeAttribute.Methods.FirstOrDefault(x=>x.Name == ".ctor" && x.HasParameters));

            var corLibrary =  Loader.Resolve((AssemblyNameReference)_typeSystem.CoreLibrary);
            _systemType = _assemblyDefinition.MainModule.ImportReference(corLibrary.MainModule.GetType("System.Type"));
            _systemDelegateType = _assemblyDefinition.MainModule.ImportReference(corLibrary.MainModule.GetType("System.MulticastDelegate"));
            _systemASyncCallbackType = _assemblyDefinition.MainModule.ImportReference(corLibrary.MainModule.GetType("System.AsyncCallback"));
            _systemIASyncResultType = _assemblyDefinition.MainModule.ImportReference(corLibrary.MainModule.GetType("System.IAsyncResult"));

            var asmDef = GetAsmDefinitionFromFile(Loader, "UnityEngine.CoreModule.dll");
            var runtimeInitializeOnLoadMethodAttribute =  asmDef.MainModule.GetType("UnityEngine", "RuntimeInitializeOnLoadMethodAttribute");
            var runtimeInitializeLoadType = asmDef.MainModule.GetType("UnityEngine", "RuntimeInitializeLoadType");

            var burstDiscardType = asmDef.MainModule.GetType("Unity.Burst", "BurstDiscardAttribute");
            _burstDiscardAttributeConstructor = _assemblyDefinition.MainModule.ImportReference(burstDiscardType.Methods.First(method => method.Name == ".ctor"));

            _unityEngineInitializeOnLoadAttributeCtor = _assemblyDefinition.MainModule.ImportReference(runtimeInitializeOnLoadMethodAttribute.Methods.FirstOrDefault(x => x.Name == ".ctor" && x.HasParameters));
            _unityEngineRuntimeInitializeLoadType = _assemblyDefinition.MainModule.ImportReference(runtimeInitializeLoadType);
            _unityEngineRuntimeInitializeLoadAfterAssemblies = runtimeInitializeLoadType.Fields.FirstOrDefault(x => x.Name=="AfterAssembliesLoaded");

            if (IsForEditor)
            {
                asmDef = GetAsmDefinitionFromFile(Loader, "UnityEditor.CoreModule.dll");
                if (asmDef == null)
                    asmDef = GetAsmDefinitionFromFile(Loader, "UnityEditor.dll");
                var initializeOnLoadMethodAttribute = asmDef.MainModule.GetType("UnityEditor", "InitializeOnLoadMethodAttribute");

                _unityEditorInitilizeOnLoadAttributeCtor = _assemblyDefinition.MainModule.ImportReference(initializeOnLoadMethodAttribute.Methods.FirstOrDefault(x => x.Name == ".ctor" && !x.HasParameters));
            }
        }

        private static void EmitArguments(ILProcessor processor, MethodDefinition method)
        {
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        processor.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        processor.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        processor.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        processor.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        if (i <= 255)
                        {
                            processor.Emit(OpCodes.Ldarg_S, (byte)i);
                        }
                        else
                        {
                            processor.Emit(OpCodes.Ldarg, i);
                        }
                        break;
                }
            }
        }

        private static bool TryGetBurstCompilerAttribute(ICustomAttributeProvider provider, out CustomAttribute customAttribute)
        {
            if (provider.HasCustomAttributes)
            {
                foreach (var customAttr in provider.CustomAttributes)
                {
                    if (customAttr.Constructor.DeclaringType.Name == "BurstCompileAttribute")
                    {
                        customAttribute = customAttr;
                        return true;
                    }
                }
            }
            customAttribute = null;
            return false;
        }
    }
}
