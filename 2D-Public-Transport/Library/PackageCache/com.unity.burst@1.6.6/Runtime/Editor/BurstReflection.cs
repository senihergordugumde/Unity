#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Compilation;
using Debug = UnityEngine.Debug;

[assembly: InternalsVisibleTo("Unity.Burst.Editor.Tests")]
namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    internal static class BurstReflection
    {
        // The TypeCache API was added in 2019.2. So there are two versions of FindExecuteMethods,
        // one that uses TypeCache and one that doesn't.
#if UNITY_2019_2_OR_NEWER
        public static FindExecuteMethodsResult FindExecuteMethods(List<System.Reflection.Assembly> assemblyList, BurstReflectionAssemblyOptions options)
        {
            var methodsToCompile = new List<BurstCompileTarget>();
            var methodsToCompileSet = new HashSet<MethodInfo>();
            var logMessages = new List<LogMessage>();
            var interfaceToProducer = new Dictionary<Type, Type>();

            var assemblySet = new HashSet<System.Reflection.Assembly>(assemblyList);

            void AddTarget(BurstCompileTarget target)
            {
                // We will not try to record more than once a method in the methods to compile
                // This can happen if a job interface is inheriting from another job interface which are using in the end the same
                // job producer type
                if (!target.IsStaticMethod && !methodsToCompileSet.Add(target.Method))
                {
                    return;
                }

                if (options.HasFlag(BurstReflectionAssemblyOptions.ExcludeTestAssemblies) &&
                    target.JobType.Assembly.GetReferencedAssemblies().Any(x => IsNUnitDll(x.Name)))
                {
                    return;
                }

                methodsToCompile.Add(target);
            }

            var staticMethodTypes = new HashSet<Type>();

            // -------------------------------------------
            // Find job structs using TypeCache.
            // -------------------------------------------

            var jobProducerImplementations = TypeCache.GetTypesWithAttribute<JobProducerTypeAttribute>();
            foreach (var jobProducerImplementation in jobProducerImplementations)
            {
                var attrs = jobProducerImplementation.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                staticMethodTypes.Add(jobProducerImplementation);

                var attr = (JobProducerTypeAttribute)attrs[0];
                interfaceToProducer.Add(jobProducerImplementation, attr.ProducerType);
            }

            foreach (var jobProducerImplementation in jobProducerImplementations)
            {
                if (!jobProducerImplementation.IsInterface)
                {
                    continue;
                }

                var jobTypes = TypeCache.GetTypesDerivedFrom(jobProducerImplementation);

                foreach (var jobType in jobTypes)
                {
                    if (jobType.IsGenericType || !jobType.IsValueType)
                    {
                        continue;
                    }

                    ScanJobType(jobType, interfaceToProducer, logMessages, AddTarget);
                }
            }

            // -------------------------------------------
            // Find static methods using TypeCache.
            // -------------------------------------------

            void AddStaticMethods(TypeCache.MethodCollection methods)
            {
                foreach (var method in methods)
                {
                    if (HasBurstCompileAttribute(method.DeclaringType))
                    {
                        staticMethodTypes.Add(method.DeclaringType);

                        // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                        // We are only working on plain type or generic type instance!
                        if (!method.DeclaringType.IsGenericTypeDefinition &&
                            method.IsStatic &&
                            !method.ContainsGenericParameters)
                        {
                            AddTarget(new BurstCompileTarget(method, method.DeclaringType, null, true));
                        }
                    }
                }
            }

            // Add [BurstCompile] static methods.
            AddStaticMethods(TypeCache.GetMethodsWithAttribute<BurstCompileAttribute>());

            // Add [TestCompiler] static methods.
            if (!options.HasFlag(BurstReflectionAssemblyOptions.ExcludeTestAssemblies))
            {
                var testCompilerAttributeType = Type.GetType("Burst.Compiler.IL.Tests.TestCompilerAttribute, Unity.Burst.Tests.UnitTests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (testCompilerAttributeType != null)
                {
                    AddStaticMethods(TypeCache.GetMethodsWithAttribute(testCompilerAttributeType));
                }
            }

            // -------------------------------------------
            // Find job types and static methods based on
            // generic instances types. These will not be
            // found by the TypeCache scanning above.
            // -------------------------------------------
            FindExecuteMethodsForGenericInstances(
                assemblySet,
                staticMethodTypes,
                interfaceToProducer,
                AddTarget,
                logMessages);

            return new FindExecuteMethodsResult(methodsToCompile, logMessages);
        }

        private static void ScanJobType(
            Type jobType,
            Dictionary<Type, Type> interfaceToProducer,
            List<LogMessage> logMessages,
            Action<BurstCompileTarget> addTarget)
        {
            foreach (var interfaceType in jobType.GetInterfaces())
            {
                var genericLessInterface = interfaceType;
                if (interfaceType.IsGenericType)
                {
                    genericLessInterface = interfaceType.GetGenericTypeDefinition();
                }

                if (interfaceToProducer.TryGetValue(genericLessInterface, out var foundProducer))
                {
                    var genericParams = new List<Type> { jobType };
                    if (interfaceType.IsGenericType)
                    {
                        genericParams.AddRange(interfaceType.GenericTypeArguments);
                    }

                    try
                    {
                        var executeType = foundProducer.MakeGenericType(genericParams.ToArray());
                        var executeMethod = executeType.GetMethod("Execute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (executeMethod == null)
                        {
                            throw new InvalidOperationException($"Burst reflection error. The type `{executeType}` does not contain an `Execute` method");
                        }

                        addTarget(new BurstCompileTarget(executeMethod, jobType, interfaceType, false));
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add(new LogMessage(ex));
                    }
                }
            }
        }

        private static void FindExecuteMethodsForGenericInstances(
            HashSet<System.Reflection.Assembly> assemblyList,
            HashSet<Type> staticMethodTypes,
            Dictionary<Type, Type> interfaceToProducer,
            Action<BurstCompileTarget> addTarget,
            List<LogMessage> logMessages)
        {
            var valueTypes = new List<TypeToVisit>();

            //Debug.Log("Filtered Assembly List: " + string.Join(", ", assemblyList.Select(assembly => assembly.GetName().Name)));

            // Find all ways to execute job types (via producer attributes)
            var typesVisited = new HashSet<string>();
            var typesToVisit = new HashSet<string>();
            var allTypesAssembliesCollected = new HashSet<Type>();
            foreach (var assembly in assemblyList)
            {
                var types = new List<Type>();
                try
                {
                    // Collect all generic type instances (excluding indirect instances)
                    CollectGenericTypeInstances(
                        assembly,
                        x => assemblyList.Contains(x.Assembly),
                        types,
                        allTypesAssembliesCollected);
                }
                catch (Exception ex)
                {
                    logMessages.Add(new LogMessage(LogType.Warning, "Unexpected exception while collecting types in assembly `" + assembly.FullName + "` Exception: " + ex));
                }

                for (var i = 0; i < types.Count; i++)
                {
                    var t = types[i];
                    if (typesToVisit.Add(t.AssemblyQualifiedName))
                    {
                        // Because the list of types returned by CollectGenericTypeInstances does not detect nested generic classes that are not
                        // used explicitly, we need to create them if a declaring type is actually used
                        // so for example if we have:
                        // class MyClass<T> { class MyNestedClass { } }
                        // class MyDerived : MyClass<int> { }
                        // The CollectGenericTypeInstances will return typically the type MyClass<int>, but will not list MyClass<int>.MyNestedClass
                        // So the following code is correcting this in order to fully query the full graph of generic instance types, including indirect types
                        var nestedTypes = t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var nestedType in nestedTypes)
                        {
                            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                            {
                                var parentGenericTypeArguments = t.GetGenericArguments();
                                // Only create nested types that are closed generic types (full generic instance types)
                                // It happens if for example the parent class is `class MClass<T> { class MyNestedGeneric<T1> {} }`
                                // In that case, MyNestedGeneric<T1> is opened in the context of MClass<int>, so we don't process them
                                if (nestedType.GetGenericArguments().Length == parentGenericTypeArguments.Length)
                                {
                                    try
                                    {
                                        var instanceNestedType = nestedType.MakeGenericType(parentGenericTypeArguments);
                                        types.Add(instanceNestedType);
                                    }
                                    catch (Exception ex)
                                    {
                                        var error = $"Unexpected Burst Inspector error. Invalid generic type instance. Trying to instantiate the generic type {nestedType.FullName} with the generic arguments <{string.Join(", ", parentGenericTypeArguments.Select(x => x.FullName))}> is not supported: {ex}";
                                        logMessages.Add(new LogMessage(LogType.Warning, error));
                                    }
                                }
                            }
                            else
                            {
                                types.Add(nestedType);
                            }
                        }
                    }
                }

                foreach (var t in types)
                {
                    // If the type has been already visited, don't try to visit it
                    if (!typesVisited.Add(t.AssemblyQualifiedName) || (t.IsGenericTypeDefinition && !t.IsInterface))
                    {
                        continue;
                    }

                    try
                    {
                        // collect methods with types having a [BurstCompile] attribute
                        var staticMethodDeclaringType = t;
                        if (t.IsGenericType)
                        {
                            staticMethodDeclaringType = t.GetGenericTypeDefinition();
                        }
                        bool visitStaticMethods = staticMethodTypes.Contains(staticMethodDeclaringType);
                        bool isValueType = false;

                        if (t.IsValueType)
                        {
                            // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                            // We are only working on plain type or generic type instance!
                            if (!t.IsGenericTypeDefinition)
                                isValueType = true;
                        }

                        if (isValueType || visitStaticMethods)
                        {
                            valueTypes.Add(new TypeToVisit(t, visitStaticMethods));
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add(new LogMessage(LogType.Warning,
                            "Unexpected exception while inspecting type `" + t +
                            "` IsConstructedGenericType: " + t.IsConstructedGenericType +
                            " IsGenericTypeDef: " + t.IsGenericTypeDefinition +
                            " IsGenericParam: " + t.IsGenericParameter +
                            " Exception: " + ex));
                    }
                }
            }

            // Revisit all types to find things that are compilable using the above producers.
            foreach (var typePair in valueTypes)
            {
                var type = typePair.Type;

                // collect static [BurstCompile] methods
                if (typePair.CollectStaticMethods)
                {
                    try
                    {
                        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var method in methods)
                        {
                            if (HasBurstCompileAttribute(method))
                            {
                                addTarget(new BurstCompileTarget(method, type, null, true));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add(new LogMessage(ex));
                    }
                }

                // If the type is not a value type, we don't need to proceed with struct Jobs
                if (!type.IsValueType)
                {
                    continue;
                }

                ScanJobType(type, interfaceToProducer, logMessages, addTarget);
            }
        }
#else
        public static FindExecuteMethodsResult FindExecuteMethods(IEnumerable<System.Reflection.Assembly> assemblyList, BurstReflectionAssemblyOptions options)
        {
            var methodsToCompile = new List<BurstCompileTarget>();
            var methodsToCompileSet = new HashSet<MethodInfo>();

            var valueTypes = new List<TypeToVisit>();
            var interfaceToProducer = new Dictionary<Type, Type>();

            // This method can be called on a background thread, so we can't call Debug.Log etc.
            // Instead collect all the log messages and return them.
            var logMessages = new List<LogMessage>();

            //Debug.Log("Filtered Assembly List: " + string.Join(", ", assemblyList.Select(assembly => assembly.GetName().Name)));

            // Find all ways to execute job types (via producer attributes)
            var typesVisited = new HashSet<string>();
            var typesToVisit = new HashSet<string>();
            var allTypesAssembliesCollected = new HashSet<Type>();
            foreach (var assembly in assemblyList)
            {
                var types = new List<Type>();
                try
                {
                    var typesFromAssembly = assembly.GetTypes();
                    types.AddRange(typesFromAssembly);
                    foreach(var typeInAssembly in typesFromAssembly)
                    {
                        allTypesAssembliesCollected.Add(typeInAssembly);
                    }

                    // Collect all generic type instances (excluding indirect instances)
                    CollectGenericTypeInstances(assembly, x => true, types, allTypesAssembliesCollected);
                }
                catch (Exception ex)
                {
                    logMessages.Add(new LogMessage(LogType.Warning, "Unexpected exception while collecting types in assembly `" + assembly.FullName + "` Exception: " + ex));
                }

                for (var i = 0; i < types.Count; i++)
                {
                    var t = types[i];
                    if (typesToVisit.Add(t.AssemblyQualifiedName))
                    {
                        // Because the list of types returned by CollectGenericTypeInstances does not detect nested generic classes that are not
                        // used explicitly, we need to create them if a declaring type is actually used
                        // so for example if we have:
                        // class MyClass<T> { class MyNestedClass { } }
                        // class MyDerived : MyClass<int> { }
                        // The CollectGenericTypeInstances will return typically the type MyClass<int>, but will not list MyClass<int>.MyNestedClass
                        // So the following code is correcting this in order to fully query the full graph of generic instance types, including indirect types
                        var nestedTypes = t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var nestedType in nestedTypes)
                        {
                            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                            {
                                var parentGenericTypeArguments = t.GetGenericArguments();
                                // Only create nested types that are closed generic types (full generic instance types)
                                // It happens if for example the parent class is `class MClass<T> { class MyNestedGeneric<T1> {} }`
                                // In that case, MyNestedGeneric<T1> is opened in the context of MClass<int>, so we don't process them
                                if (nestedType.GetGenericArguments().Length == parentGenericTypeArguments.Length)
                                {
                                    try
                                    {
                                        var instanceNestedType = nestedType.MakeGenericType(parentGenericTypeArguments);
                                        types.Add(instanceNestedType);
                                    }
                                    catch (Exception ex)
                                    {
                                        var error = $"Unexpected Burst Inspector error. Invalid generic type instance. Trying to instantiate the generic type {nestedType.FullName} with the generic arguments <{string.Join(", ", parentGenericTypeArguments.Select(x => x.FullName))}> is not supported: {ex}";
                                        logMessages.Add(new LogMessage(LogType.Warning, error));
                                    }
                                }
                            }
                            else
                            {
                                types.Add(nestedType);
                            }
                        }
                    }
                }

                foreach (var t in types)
                {
                    // If the type has been already visited, don't try to visit it
                    if (!typesVisited.Add(t.AssemblyQualifiedName) || (t.IsGenericTypeDefinition && !t.IsInterface))
                    {
                        continue;
                    }

                    try
                    {
                        // collect methods with types having a [BurstCompile] attribute
                        bool visitStaticMethods = HasBurstCompileAttribute(t);
                        bool isValueType = false;
                        
                        if (t.IsInterface)
                        {
                            object[] attrs = t.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                            if (attrs.Length == 0)
                                continue;

                            JobProducerTypeAttribute attr = (JobProducerTypeAttribute)attrs[0];

                            interfaceToProducer.Add(t, attr.ProducerType);

                            //Debug.Log($"{t} has producer {attr.ProducerType}");
                        }
                        else if (t.IsValueType)
                        {
                            // NOTE: Make sure that we don't use a value type generic definition (e.g `class Outer<T> { struct Inner { } }`)
                            // We are only working on plain type or generic type instance!
                            if (!t.IsGenericTypeDefinition)
                                isValueType = true;
                        }

                        if (isValueType || visitStaticMethods)
                        {
                            valueTypes.Add(new TypeToVisit(t, visitStaticMethods));
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add(new LogMessage(LogType.Warning,
                            "Unexpected exception while inspecting type `" + t +
                            "` IsConstructedGenericType: " + t.IsConstructedGenericType +
                            " IsGenericTypeDef: " + t.IsGenericTypeDefinition +
                            " IsGenericParam: " + t.IsGenericParameter +
                            " Exception: " + ex));
                    }
                }
            }

            //Debug.Log($"Mapped {interfaceToProducer.Count} producers; {valueTypes.Count} value types");

            // Revisit all types to find things that are compilable using the above producers.
            foreach (var typePair in valueTypes)
            {
                var type = typePair.Type;

                // collect static [BurstCompile] methods
                if (typePair.CollectStaticMethods)
                {
                    try
                    {
                        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var method in methods)
                        {
                            if (!method.ContainsGenericParameters && HasBurstCompileAttribute(method))
                            {
                                var target = new BurstCompileTarget(method, type, null, true);
                                methodsToCompile.Add(target);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessages.Add(new LogMessage(ex));
                    }
                }

                // If the type is not a value type, we don't need to proceed with struct Jobs
                if (!type.IsValueType)
                {
                    continue;
                }

                // Otherwise try to find if we have an interface producer setup on the class
                foreach (var interfaceType in type.GetInterfaces())
                {
                    var genericLessInterface = interfaceType;
                    if (interfaceType.IsGenericType)
                    {
                        genericLessInterface = interfaceType.GetGenericTypeDefinition();
                    }

                    Type foundProducer;
                    if (interfaceToProducer.TryGetValue(genericLessInterface, out foundProducer))
                    {
                        var genericParams = new List<Type> {type};
                        if (interfaceType.IsGenericType)
                        {
                            genericParams.AddRange(interfaceType.GenericTypeArguments);
                        }

                        try
                        {
                            var executeType = foundProducer.MakeGenericType(genericParams.ToArray());
                            var executeMethod = executeType.GetMethod("Execute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            if (executeMethod == null)
                            {
                                throw new InvalidOperationException($"Burst reflection error. The type `{executeType}` does not contain an `Execute` method");
                            }

                            // We will not try to record more than once a method in the methods to compile
                            // This can happen if a job interface is inheriting from another job interface which are using in the end the same
                            // job producer type
                            if (methodsToCompileSet.Add(executeMethod))
                            {
                                var target = new BurstCompileTarget(executeMethod, type, interfaceType, false);
                                methodsToCompile.Add(target);
                            }
                        }
                        catch (Exception ex)
                        {
                            logMessages.Add(new LogMessage(ex));
                        }
                    }
                }
            }

            return new FindExecuteMethodsResult(methodsToCompile, logMessages);
        }
#endif

        public sealed class FindExecuteMethodsResult
        {
            public readonly List<BurstCompileTarget> CompileTargets;
            public readonly List<LogMessage> LogMessages;

            public FindExecuteMethodsResult(List<BurstCompileTarget> compileTargets, List<LogMessage> logMessages)
            {
                CompileTargets = compileTargets;
                LogMessages = logMessages;
            }
        }

        public sealed class LogMessage
        {
            public readonly LogType LogType;
            public readonly string Message;
            public readonly Exception Exception;

            public LogMessage(LogType logType, string message)
            {
                LogType = logType;
                Message = message;
            }

            public LogMessage(Exception exception)
            {
                LogType = LogType.Exception;
                Exception = exception;
            }
        }

        public enum LogType
        {
            Warning,
            Exception,
        }

        /// <summary>
        /// This method exists solely to ensure that the static constructor has been called.
        /// </summary>
        public static void EnsureInitialized() { }

#if !UNITY_2019_3_OR_NEWER
        // This field is only used for Unity 2018.4.
        public static readonly List<System.Reflection.Assembly> AllEditorAssemblies;
#endif
        public static readonly List<System.Reflection.Assembly> EditorAssembliesThatCanPossiblyContainJobs;
        public static readonly List<System.Reflection.Assembly> EditorAssembliesThatCanPossiblyContainJobsExcludingTestAssemblies;

        /// <summary>
        /// Collects (and caches) all editor assemblies - transitively.
        /// </summary>
        static BurstReflection()
        {
#if !UNITY_2019_3_OR_NEWER
            AllEditorAssemblies = new List<System.Reflection.Assembly>();
#endif
            EditorAssembliesThatCanPossiblyContainJobs = new List<System.Reflection.Assembly>();
            EditorAssembliesThatCanPossiblyContainJobsExcludingTestAssemblies = new List<System.Reflection.Assembly>();

            // TODO: Not sure there is a better way to match assemblies returned by CompilationPipeline.GetAssemblies
            // with runtime assemblies contained in the AppDomain.CurrentDomain.GetAssemblies()

            // Filter the assemblies
            var assemblyList = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            var assemblyNames = new HashSet<string>();
            foreach (var assembly in assemblyList)
            {
                CollectAssemblyNames(assembly, assemblyNames);
            }

            var allAssemblies = new HashSet<System.Reflection.Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assemblyNames.Contains(assembly.GetName().Name))
                {
                    continue;
                }
                CollectAssembly(assembly, allAssemblies);
            }
        }

        // For an assembly to contain something "interesting" when we're scanning for things to compile,
        // it needs to either:
        // (a) be one of these assemblies, or
        // (b) reference one of these assemblies
        private static readonly string[] ScanMarkerAssemblies = new[]
        {
            // Contains [BurstCompile] attribute
            "Unity.Burst",

            // Contains [JobProducerType] attribute
            "UnityEngine.CoreModule"
        };

        private static void CollectAssembly(System.Reflection.Assembly assembly, HashSet<System.Reflection.Assembly> collect)
        {
            if (!collect.Add(assembly))
            {
                return;
            }

#if !UNITY_2019_3_OR_NEWER
            AllEditorAssemblies.Add(assembly);
#endif

            var referencedAssemblies = assembly.GetReferencedAssemblies();

#if UNITY_2019_3_OR_NEWER
            var shouldCollectReferences = false;
#endif

            var name = assembly.GetName().Name;
            if (ScanMarkerAssemblies.Contains(name) || referencedAssemblies.Any(x => ScanMarkerAssemblies.Contains(x.Name)))
            {
                EditorAssembliesThatCanPossiblyContainJobs.Add(assembly);
#if UNITY_2019_3_OR_NEWER
                shouldCollectReferences = true;
#endif

                if (!assembly.GetReferencedAssemblies().Any(x => IsNUnitDll(x.Name)))
                {
                    EditorAssembliesThatCanPossiblyContainJobsExcludingTestAssemblies.Add(assembly);
                }
            }

#if UNITY_2019_3_OR_NEWER
            // We can only do this optimization on Unity 2019.3+.
            // For Unity 2018.4, we need to populate AllEditorAssemblies
            // because it is used in BurstLoader.
            if (!shouldCollectReferences)
            {
                return;
            }
#endif

            foreach (var assemblyName in referencedAssemblies)
            {
                try
                {
                    CollectAssembly(System.Reflection.Assembly.Load(assemblyName), collect);
                }
                catch (Exception)
                {
                    if (BurstLoader.IsDebugging)
                    {
                        Debug.LogWarning("Could not load assembly " + assemblyName);
                    }
                }
            }
        }

        private static bool IsNUnitDll(string value)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(value, "nunit.framework") >= 0;
        }

        private static void CollectAssemblyNames(UnityEditor.Compilation.Assembly assembly, HashSet<string> collect)
        {
            if (assembly == null || assembly.name == null) return;

            if (!collect.Add(assembly.name))
            {
                return;
            }

            foreach (var assemblyRef in assembly.assemblyReferences)
            {
                CollectAssemblyNames(assemblyRef, collect);
            }
        }

        /// <summary>
        /// Gets the list of concrete generic type instances used in an assembly.
        /// See remarks
        /// </summary>
        /// <param name="assembly">The assembly</param>
        /// <param name="types"></param>
        /// <returns>The list of generic type instances</returns>
        /// <remarks>
        /// Note that this method fetchs only direct type instances but
        /// cannot fetch transitive generic type instances.
        /// </remarks>
        private static void CollectGenericTypeInstances(
            System.Reflection.Assembly assembly,
            Func<Type, bool> typeFilter,
            List<Type> types,
            HashSet<Type> visited)
        {
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // WARNING: THIS CODE HAS TO BE MAINTAINED IN SYNC WITH BclApp.cs
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // From: https://gist.github.com/xoofx/710aaf86e0e8c81649d1261b1ef9590e
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            const int mdMaxCount = 1 << 24;
            foreach (var module in assembly.Modules)
            {
                for (int i = 1; i < mdMaxCount; i++)
                {
                    try
                    {
                        // Token base id for TypeSpec
                        const int mdTypeSpec = 0x1B000000;
                        var type = module.ResolveType(mdTypeSpec | i);
                        CollectGenericTypeInstances(type, types, visited, typeFilter);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    catch (ArgumentException)
                    {
                        // Can happen on ResolveType on certain generic types, so we continue
                    }
                }

                for (int i = 1; i < mdMaxCount; i++)
                {
                    try
                    {
                        // Token base id for MethodSpec
                        const int mdMethodSpec = 0x2B000000;
                        var method = module.ResolveMethod(mdMethodSpec | i);
                        var genericArgs = method.GetGenericArguments();
                        foreach (var genArgType in genericArgs)
                        {
                            CollectGenericTypeInstances(genArgType, types, visited, typeFilter);
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    catch (ArgumentException)
                    {
                        // Can happen on ResolveType on certain generic types, so we continue
                    }
                }

                for (int i = 1; i < mdMaxCount; i++)
                {
                    try
                    {
                        // Token base id for Field
                        const int mdField = 0x04000000;
                        var field = module.ResolveField(mdField | i);
                        CollectGenericTypeInstances(field.FieldType, types, visited, typeFilter);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    catch (ArgumentException)
                    {
                        // Can happen on ResolveType on certain generic types, so we continue
                    }
                }
            }

            // Scan for types used in constructor arguments to assembly-level attributes,
            // such as [RegisterGenericJobType(typeof(...))].
            foreach (var customAttribute in assembly.CustomAttributes)
            {
                foreach (var argument in customAttribute.ConstructorArguments)
                {
                    if (argument.ArgumentType == typeof(Type))
                    {
                        CollectGenericTypeInstances((Type)argument.Value, types, visited, typeFilter);
                    }
                }
            }
        }

        private static void CollectGenericTypeInstances(
            Type type,
            List<Type> types,
            HashSet<Type> visited,
            Func<Type, bool> typeFilter)
        {
            if (type.IsPrimitive) return;
            if (!visited.Add(type)) return;

            // Add only concrete types
            if (type.IsConstructedGenericType && !type.ContainsGenericParameters && typeFilter(type))
            {
                types.Add(type);
            }

            // Collect recursively generic type arguments
            var genericTypeArguments = type.GenericTypeArguments;
            foreach (var genericTypeArgument in genericTypeArguments)
            {
                if (!genericTypeArgument.IsPrimitive)
                {
                    CollectGenericTypeInstances(genericTypeArgument, types, visited, typeFilter);
                }
            }
        }

        [DebuggerDisplay("{Type} (static methods: {CollectStaticMethods})")]
        private struct TypeToVisit
        {
            public TypeToVisit(Type type, bool collectStaticMethods)
            {
                Type = type;
                CollectStaticMethods = collectStaticMethods;
            }

            public readonly Type Type;

            public readonly bool CollectStaticMethods;
        }
    }

    [Flags]
    internal enum BurstReflectionAssemblyOptions
    {
        None = 0,
        ExcludeTestAssemblies = 1,
    }
}
#endif