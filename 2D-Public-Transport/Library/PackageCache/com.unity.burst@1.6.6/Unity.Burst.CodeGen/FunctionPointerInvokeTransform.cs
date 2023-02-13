using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace zzzUnity.Burst.CodeGen
{
    /// <summary>
    /// Transforms a direct invoke on a burst function pointer into an calli, avoiding the need to marshal the delegate back.
    /// </summary>
    internal class FunctionPointerInvokeTransform
    {
        private struct CaptureInformation
        {
            public MethodReference Operand;

            public List<Instruction> Captured;
        }

        private Dictionary<TypeReference, (MethodDefinition method, Instruction instruction)> _needsNativeFunctionPointer;
        private Dictionary<MethodDefinition, TypeReference> _needsIl2cppInvoke;
        private Dictionary<MethodDefinition, List<CaptureInformation>> _capturedSets;
        private MethodDefinition _monoPInvokeAttributeCtorDef;
        private MethodDefinition _nativePInvokeAttributeCtorDef;
        private MethodDefinition _unmanagedFunctionPointerAttributeCtorDef;
        private TypeReference _burstFunctionPointerType;
        private TypeReference _burstCompilerType;
        private TypeReference _systemType;
        private TypeReference _callingConventionType;

        private LogDelegate _debugLog;
        private int _logLevel;

        private AssemblyLoader _loader;

        private ErrorDiagnosticDelegate _errorReport;

        public readonly static bool enableInvokeAttribute = true;
#if UNITY_DOTSPLAYER
        public readonly static bool enableCalliOptimisation = true;
        public readonly static bool enableUnmangedFunctionPointerInject = false;
#else
        public readonly static bool enableCalliOptimisation = false;		// For now only run the pass on dots player/tiny
        public readonly static bool enableUnmangedFunctionPointerInject = true;
#endif

        public FunctionPointerInvokeTransform(AssemblyLoader loader,ErrorDiagnosticDelegate error, LogDelegate log = null, int logLevel = 0)
        {
            _loader = loader;

            _needsNativeFunctionPointer = new Dictionary<TypeReference, (MethodDefinition, Instruction)>();
            _needsIl2cppInvoke = new Dictionary<MethodDefinition, TypeReference>();
            _capturedSets = new Dictionary<MethodDefinition, List<CaptureInformation>>();
            _monoPInvokeAttributeCtorDef = null;
            _unmanagedFunctionPointerAttributeCtorDef = null;
            _nativePInvokeAttributeCtorDef = null;  // Only present on DOTS_PLAYER
            _burstFunctionPointerType = null;
            _burstCompilerType = null;
            _systemType = null;
            _callingConventionType = null;
            _debugLog = log;
            _logLevel = logLevel;
            _errorReport = error;
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

        public void Initialize(AssemblyLoader loader, AssemblyDefinition assemblyDefinition, TypeSystem typeSystem)
        {
            if (_monoPInvokeAttributeCtorDef == null)
            {
                var burstAssembly = GetAsmDefinitionFromFile(loader, "Unity.Burst.dll");

                _burstFunctionPointerType = burstAssembly.MainModule.GetType("Unity.Burst.FunctionPointer`1");
                _burstCompilerType = burstAssembly.MainModule.GetType("Unity.Burst.BurstCompiler");

                var corLibrary = loader.Resolve(typeSystem.CoreLibrary as AssemblyNameReference);
                _systemType = corLibrary.MainModule.Types.FirstOrDefault(x => x.FullName == "System.Type"); // Only needed for MonoPInvokeCallback constructor in Unity

                if (enableUnmangedFunctionPointerInject)
                {
                    var unmanagedFunctionPointerAttribute = corLibrary.MainModule.GetType("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");
                    _callingConventionType = corLibrary.MainModule.GetType("System.Runtime.InteropServices.CallingConvention");
                    _unmanagedFunctionPointerAttributeCtorDef = unmanagedFunctionPointerAttribute.GetConstructors().Single(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.MetadataType == _callingConventionType.MetadataType);
                }

#if UNITY_DOTSPLAYER
                var asmDef = assemblyDefinition;
                if (asmDef.Name.Name != "Unity.Runtime")
                    asmDef = GetAsmDefinitionFromFile(loader, "Unity.Runtime.dll");

                if (asmDef == null)
                    return;

                var monoPInvokeAttribute = asmDef.MainModule.GetType("Unity.Jobs.MonoPInvokeCallbackAttribute");
                _monoPInvokeAttributeCtorDef = monoPInvokeAttribute.GetConstructors().First();

                var nativePInvokeAttribute = asmDef.MainModule.GetType("NativePInvokeCallbackAttribute");
                _nativePInvokeAttributeCtorDef = nativePInvokeAttribute.GetConstructors().First();
#else
                var asmDef = GetAsmDefinitionFromFile(loader, "UnityEngine.CoreModule.dll");
                // bail if we can't find a reference, handled gracefully later
                if (asmDef == null)
                    return;

                var monoPInvokeAttribute = asmDef.MainModule.GetType("AOT.MonoPInvokeCallbackAttribute");
                _monoPInvokeAttributeCtorDef = monoPInvokeAttribute.GetConstructors().First();
#endif
            }

        }

        public bool Run(AssemblyDefinition assemblyDefinition)
        {
            Initialize(_loader, assemblyDefinition, assemblyDefinition.MainModule.TypeSystem);

            var types = assemblyDefinition.MainModule.GetTypes().ToArray();
            foreach (var type in types)
            {
                CollectDelegateInvokesFromType(type);
            }

            return Finish();
        }

        public void CollectDelegateInvokesFromType(TypeDefinition type)
        {
            foreach (var m in type.Methods)
            {
                if (m.HasBody)
                {
                    CollectDelegateInvokes(m);
                }
            }
        }

        private bool ProcessUnmanagedAttributeFixups()
        {
            if (_unmanagedFunctionPointerAttributeCtorDef == null)
                return false;

            bool modified = false;

            foreach (var kp in _needsNativeFunctionPointer)
            {
                var delegateType = kp.Key;
                var instruction = kp.Value.instruction;
                var method = kp.Value.method;
                var delegateDef = delegateType.Resolve();

                var hasAttributeAlready = delegateDef.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == _unmanagedFunctionPointerAttributeCtorDef.DeclaringType.FullName);

                // If there is already an an attribute present
                if (hasAttributeAlready!=null)
                {
                    if (hasAttributeAlready.ConstructorArguments.Count==1)
                    {
                        var cc = (System.Runtime.InteropServices.CallingConvention)hasAttributeAlready.ConstructorArguments[0].Value;
                        if (cc == System.Runtime.InteropServices.CallingConvention.Cdecl)
                        {
                            if (_logLevel > 2) _debugLog?.Invoke($"UnmanagedAttributeFixups Skipping appending unmanagedFunctionPointerAttribute as already present aand calling convention matches");
                        }
                        else
                        {
                            // constructor with non cdecl calling convention
                            _errorReport(method, instruction, $"BurstCompiler.CompileFunctionPointer is only compatible with cdecl calling convention, this delegate type already has `[UnmanagedFunctionPointer(CallingConvention.{ Enum.GetName(typeof(System.Runtime.InteropServices.CallingConvention), cc) })]` please remove the attribute if you wish to use this function with Burst.");
                        }
                    }
                    else
                    {
                        // Empty constructor which defaults to Winapi which is incompatable
                        _errorReport(method, instruction, $"BurstCompiler.CompileFunctionPointer is only compatible with cdecl calling convention, this delegate type already has `[UnmanagedFunctionPointer]` please remove the attribute if you wish to use this function with Burst.");
                    }
                    continue;
                }

                var attribute = new CustomAttribute(delegateType.Module.ImportReference(_unmanagedFunctionPointerAttributeCtorDef));
                attribute.ConstructorArguments.Add(new CustomAttributeArgument(delegateType.Module.ImportReference(_callingConventionType), System.Runtime.InteropServices.CallingConvention.Cdecl));
                delegateDef.CustomAttributes.Add(attribute);
                modified = true;
            }

            return modified;
        }

        private bool ProcessIl2cppInvokeFixups()
        {
            if (_monoPInvokeAttributeCtorDef == null)
                return false;

            bool modified = false;
            foreach (var invokeNeeded in _needsIl2cppInvoke)
            {
                var declaringType = invokeNeeded.Value;
                var implementationMethod = invokeNeeded.Key;

#if UNITY_DOTSPLAYER

                // At present always uses monoPInvokeAttribute because we don't currently know if the burst is enabled
                var attribute = new CustomAttribute(implementationMethod.Module.ImportReference(_monoPInvokeAttributeCtorDef));
                implementationMethod.CustomAttributes.Add(attribute);
                modified = true;

#else
                // Unity requires a type parameter for the attributecallback
                if (declaringType == null)
                {
                    _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Unable to automatically append CallbackAttribute due to missing declaringType for {implementationMethod}");
                    continue;
                }

                var attribute = new CustomAttribute(implementationMethod.Module.ImportReference(_monoPInvokeAttributeCtorDef));
                attribute.ConstructorArguments.Add(new CustomAttributeArgument(implementationMethod.Module.ImportReference(_systemType), implementationMethod.Module.ImportReference(declaringType)));
                implementationMethod.CustomAttributes.Add(attribute);
                modified = true;

                if (_logLevel > 1) _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Added InvokeCallbackAttribute to  {implementationMethod}");
#endif
            }

            return modified;
        }

        private bool ProcessFunctionPointerInvokes()
        {
            var madeChange = false;
            foreach (var capturedData in _capturedSets)
            {
                var latePatchMethod = capturedData.Key;
                var capturedList = capturedData.Value;

                latePatchMethod.Body.SimplifyMacros();  // De-optimise short branches, since we will end up inserting instructions

                foreach(var capturedInfo in capturedList)
                {
                    var captured = capturedInfo.Captured;
                    var operand = capturedInfo.Operand;

                    if (captured.Count!=2)
                    {
                        _debugLog?.Invoke($"FunctionPtrInvoke.Finish: expected 2 instructions - Unable to optimise this reference");
                        continue;
                    }

                    if (_logLevel > 1) _debugLog?.Invoke($"FunctionPtrInvoke.Finish:{Environment.NewLine}  latePatchMethod:{latePatchMethod}{Environment.NewLine}  captureList:{capturedList}{Environment.NewLine}  capture0:{captured[0]}{Environment.NewLine}  operand:{operand}");

                    var processor = latePatchMethod.Body.GetILProcessor();

                    var callsite = new CallSite(operand.ReturnType) { CallingConvention = MethodCallingConvention.C };

                    for (int oo = 0; oo < operand.Parameters.Count; oo++)
                    {
                        callsite.Parameters.Add(operand.Parameters[oo]);
                    }

                    // Make sure everything is in order before we make a change

                    var originalGetInvoke = captured[0];

                    if (originalGetInvoke.Operand is MethodReference mmr)
                    {
                        var genericMethodDef = mmr.Resolve();

                        var genericInstanceType = mmr.DeclaringType as GenericInstanceType;
                        var genericInstanceDef = genericInstanceType.Resolve();

                        // Locate the correct instance method - we know already at this point we have an instance of Function
                        MethodReference mr = default;
                        bool failed = true;
                        foreach (var m in genericInstanceDef.Methods)
                        {
                            if (m.FullName.Contains("get_Value"))
                            {
                                mr = m;
                                failed = false;
                                break;
                            }
                        }
                        if (failed)
                        {
                            _debugLog?.Invoke($"FunctionPtrInvoke.Finish: failed to locate get_Value method on {genericInstanceDef} - Unable to optimise this reference");
                            continue;
                        }

                        var newGenericRef = new MethodReference(mr.Name, mr.ReturnType, genericInstanceType)
                        {
                            HasThis = mr.HasThis,
                            ExplicitThis = mr.ExplicitThis,
                            CallingConvention = mr.CallingConvention
                        };
                        foreach (var param in mr.Parameters)
                            newGenericRef.Parameters.Add(new ParameterDefinition(param.ParameterType));
                        foreach (var gparam in mr.GenericParameters)
                            newGenericRef.GenericParameters.Add(new GenericParameter(gparam.Name, newGenericRef));
                        var importRef = latePatchMethod.Module.ImportReference(newGenericRef);
                        var newMethodCall = processor.Create(OpCodes.Call, importRef);

                        // Replace get_invoke with get_Value - Don't use replace though as if the original call is target of a branch
                        //the branch doesn't get updated.
                        originalGetInvoke.OpCode = newMethodCall.OpCode;
                        originalGetInvoke.Operand = newMethodCall.Operand;

                        // Add local to capture result
                        var newLocal = new VariableDefinition(mr.ReturnType);
                        latePatchMethod.Body.Variables.Add(newLocal);

                        // Store result of get_Value
                        var storeInst = processor.Create(OpCodes.Stloc, newLocal);
                        processor.InsertAfter(originalGetInvoke, storeInst);

                        // Swap invoke with calli
                        var calli = processor.Create(OpCodes.Calli, callsite);
                        // We can use replace here, since we already checked this is in the same Basic Block, and thus can't be target of a branch
                        processor.Replace(captured[1], calli);

                        // Insert load local prior to calli
                        var loadValue = processor.Create(OpCodes.Ldloc, newLocal);
                        processor.InsertBefore(calli, loadValue);

                        if (_logLevel > 1) _debugLog?.Invoke($"FunctionPtrInvoke.Finish: Optimised {originalGetInvoke} with {newMethodCall}");

                        madeChange = true;
                    }
                }

                latePatchMethod.Body.OptimizeMacros();  // Re-optimise branches
            }
            return madeChange;
        }

        public bool Finish()
        {
            bool madeChange = false;

            if (enableInvokeAttribute)
            {
                madeChange |= ProcessIl2cppInvokeFixups();
            }

            if (enableUnmangedFunctionPointerInject)
            {
                madeChange |= ProcessUnmanagedAttributeFixups();
            }

            if (enableCalliOptimisation)
            {
                madeChange |= ProcessFunctionPointerInvokes();
            }

            return madeChange;
        }

        private bool IsBurstFunctionPointerMethod(MethodReference methodRef, string method, out GenericInstanceType methodInstance)
        {
            methodInstance = methodRef?.DeclaringType as GenericInstanceType;
            return (methodInstance != null && methodInstance.ElementType.FullName == _burstFunctionPointerType.FullName && methodRef.Name == method);
        }

        private bool IsBurstCompilerMethod(GenericInstanceMethod methodRef, string method)
        {
            var methodInstance = methodRef?.DeclaringType as TypeReference;
            return (methodInstance != null && methodInstance.FullName == _burstCompilerType.FullName && methodRef.Name == method);
        }

        private void LocateFunctionPointerTCreation(MethodDefinition m, Instruction i)
        {
            if (i.OpCode == OpCodes.Call)
            {
                var genInstMethod = i.Operand as GenericInstanceMethod;
                if (!IsBurstCompilerMethod(genInstMethod, "CompileFunctionPointer")) return;

                if (enableUnmangedFunctionPointerInject)
                {
                    var delegateType = genInstMethod.GenericArguments[0].Resolve();
                    // We check for null, since unfortunately it is possible that the call is wrapped inside
                    //another open delegate and we cannot determine the delegate type
                    if (delegateType != null && !_needsNativeFunctionPointer.ContainsKey(delegateType))
                    {
                        _needsNativeFunctionPointer.Add(delegateType, (m,i));
                    }
                }

                if (enableInvokeAttribute)
                {
                    // Currently only handles the following pre-pattern (which should cover most common uses)
                    // ldftn ...
                    // newobj ...

                    if (i.Previous?.OpCode != OpCodes.Newobj)
                    {
                        _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Unable to automatically append CallbackAttribute due to not finding NewObj {i.Previous}");
                        return;
                    }

                    var newObj = i.Previous;
                    if (newObj.Previous?.OpCode != OpCodes.Ldftn)
                    {
                        _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Unable to automatically append CallbackAttribute due to not finding LdFtn {newObj.Previous}");
                        return;
                    }

                    var ldFtn = newObj.Previous;

                    // Determine the delegate type
                    var methodDefinition = newObj.Operand as MethodDefinition;
                    var declaringType = methodDefinition?.DeclaringType;

                    // Fetch the implementation method
                    var implementationMethod = ldFtn.Operand as MethodDefinition;

                    var hasInvokeAlready = implementationMethod?.CustomAttributes.FirstOrDefault(x =>
                         (x.AttributeType.FullName == _monoPInvokeAttributeCtorDef.DeclaringType.FullName)
                         || (_nativePInvokeAttributeCtorDef != null && x.AttributeType.FullName == _nativePInvokeAttributeCtorDef.DeclaringType.FullName));

                    if (hasInvokeAlready != null)
                    {
                        if (_logLevel > 2) _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Skipping appending Callback Attribute as already present {hasInvokeAlready}");
                        return;
                    }

                    if (implementationMethod == null)
                    {
                        _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Unable to automatically append CallbackAttribute due to missing method from {ldFtn} {ldFtn.Operand}");
                        return;
                    }

                    if (implementationMethod.CustomAttributes.FirstOrDefault(x => x.Constructor.DeclaringType.Name == "BurstCompileAttribute") == null)
                    {
                        _debugLog?.Invoke($"FunctionPtrInvoke.LocateFunctionPointerTCreation: Unable to automatically append CallbackAttribute due to missing burst attribute from {implementationMethod}");
                        return;
                    }

                    // Need to add the custom attribute
                    if (!_needsIl2cppInvoke.ContainsKey(implementationMethod))
                    {
                        _needsIl2cppInvoke.Add(implementationMethod, declaringType);
                    }
                }
            }
        }

        [Obsolete("Will be removed in a future Burst verison")]
        public bool IsInstructionForFunctionPointerInvoke(MethodDefinition m, Instruction i)
        {
            throw new NotImplementedException();
        }

        private void CollectDelegateInvokes(MethodDefinition m)
        {
            if (!(enableCalliOptimisation || enableInvokeAttribute || enableUnmangedFunctionPointerInject))
                return;

            bool hitGetInvoke = false;
            TypeDefinition delegateType = null;
            List<Instruction> captured = null;

            foreach (var inst in m.Body.Instructions)
            {
                if (_logLevel > 2) _debugLog?.Invoke($"FunctionPtrInvoke.CollectDelegateInvokes: CurrentInstruction {inst} {inst.Operand}");

                // Check for a FunctionPointerT creation
                if (enableUnmangedFunctionPointerInject || enableInvokeAttribute)
                {
                    LocateFunctionPointerTCreation(m, inst);
                }

                if (enableCalliOptimisation)
                {
                    if (!hitGetInvoke)
                    {
                        if (inst.OpCode != OpCodes.Call) continue;
                        if (!IsBurstFunctionPointerMethod(inst.Operand as MethodReference, "get_Invoke", out var methodInstance)) continue;

                        // At this point we have a call to a FunctionPointer.Invoke
                        hitGetInvoke = true;

                        delegateType = methodInstance.GenericArguments[0].Resolve();

                        captured = new List<Instruction>();

                        captured.Add(inst); // Capture the get_invoke, we will swap this for get_value and a store to local
                    }
                    else
                    {
                        if (!(inst.OpCode.FlowControl == FlowControl.Next || inst.OpCode.FlowControl == FlowControl.Call))
                        {
                            // Don't perform transform across blocks
                            hitGetInvoke = false;
                        }
                        else
                        {
                            if (inst.OpCode == OpCodes.Callvirt)
                            {
                                if (inst.Operand is MethodReference mref)
                                {
                                    var method = mref.Resolve();

                                    if (method.DeclaringType == delegateType)
                                    {
                                        hitGetInvoke = false;

                                        List<CaptureInformation> storage = null;
                                        if (!_capturedSets.TryGetValue(m, out storage))
                                        {
                                            storage = new List<CaptureInformation>();
                                            _capturedSets.Add(m, storage);
                                        }

                                        // Capture the invoke - which we will swap for a load local (stored from the get_value) and a calli
                                        captured.Add(inst);
                                        var captureInfo = new CaptureInformation { Captured = captured, Operand = mref };
                                        if (_logLevel > 1) _debugLog?.Invoke($"FunctionPtrInvoke.CollectDelegateInvokes: captureInfo:{captureInfo}{Environment.NewLine}capture0{captured[0]}");
                                        storage.Add(captureInfo);
                                    }
                                }
                                else
                                {
                                    hitGetInvoke = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
