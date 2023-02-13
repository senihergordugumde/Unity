#if UNITY_2019_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

/// Deliberately named zzzUnity.Burst.CodeGen, as we need to ensure its last in the chain
namespace zzzUnity.Burst.CodeGen
{
    /// <summary>
    /// Postprocessor used to replace calls from C# to [BurstCompile] functions to direct calls to
    /// Burst native code functions without having to go through a C# delegate.
    /// </summary>
    internal class BurstILPostProcessor : ILPostProcessor
    {
        public bool IsDebugging;
        public int DebuggingLevel;

        private void SetupDebugging()
        {
            // This can be setup to get more diagnostics
            var debuggingStr = Environment.GetEnvironmentVariable("UNITY_BURST_DEBUG");
            IsDebugging = debuggingStr != null;
            if (IsDebugging)
            {
                Log("[com.unity.burst] Extra debugging is turned on.");
                int debuggingLevel;
                int.TryParse(debuggingStr, out debuggingLevel);
                if (debuggingLevel <= 0) debuggingLevel = 1;
                DebuggingLevel = debuggingLevel;
            }
        }

        private static SequencePoint FindBestSequencePointFor(MethodDefinition method, Instruction instruction)
        {
            var sequencePoints = method.DebugInformation?.GetSequencePointMapping().Values.OrderBy(s => s.Offset).ToList();
            if (sequencePoints == null || !sequencePoints.Any())
                return null;

            for (int i = 0; i != sequencePoints.Count-1; i++)
            {
                if (sequencePoints[i].Offset < instruction.Offset &&
                    sequencePoints[i + 1].Offset > instruction.Offset)
                    return sequencePoints[i];
            }

            return sequencePoints.FirstOrDefault();
        }

        private static DiagnosticMessage MakeDiagnosticError(MethodDefinition method, Instruction errorLocation, string message)
        {
            var m = new DiagnosticMessage { DiagnosticType = DiagnosticType.Error };
            var sPoint = errorLocation != null ? FindBestSequencePointFor(method, errorLocation) : null;
            if (sPoint!=null)
            {
                m.Column = sPoint.StartColumn;
                m.Line = sPoint.StartLine;
                m.File = sPoint.Document.Url;
            }
            m.MessageData = message;
            return m;
        }

        public override unsafe ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();
            if (!WillProcess(compiledAssembly))
                return new ILPostProcessResult(null, diagnostics);

            bool wasModified = false;
            SetupDebugging();
            bool debugging = IsDebugging && DebuggingLevel >= 2;

            var inMemoryAssembly = compiledAssembly.InMemoryAssembly;


            var peData = inMemoryAssembly.PeData;
            var pdbData = inMemoryAssembly.PdbData;


            var loader = new AssemblyLoader();
            var folders = new HashSet<string>();
            var isForEditor = compiledAssembly.Defines?.Contains("UNITY_EDITOR") ?? false;
            foreach (var reference in compiledAssembly.References)
            {
                folders.Add(Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(reference)));
            }
            var folderList = folders.OrderBy(x => x).ToList();
            foreach (var folder in folderList)
            {
                loader.AddSearchDirectory(folder);
            }

            var clock = Stopwatch.StartNew();
            if (debugging)
            {
                Log($"Start processing assembly {compiledAssembly.Name}, IsForEditor: {isForEditor}, Folders: {string.Join("\n", folderList)}");
            }

            var ilPostProcessing = new ILPostProcessing(loader, isForEditor,
                (m,i,s) => { diagnostics.Add(MakeDiagnosticError(m, i, s)); },
                IsDebugging ? Log : (LogDelegate)null, DebuggingLevel);
            var functionPointerProcessing = new FunctionPointerInvokeTransform(loader, 
                (m,i,s) => { diagnostics.Add(MakeDiagnosticError(m, i, s)); },
                IsDebugging ? Log : (LogDelegate)null, DebuggingLevel);
            try
            {
                // For IL Post Processing, use the builtin symbol reader provider
                var assemblyDefinition = loader.LoadFromStream(new MemoryStream(peData), new MemoryStream(pdbData), new PortablePdbReaderProvider() );
                wasModified |= ilPostProcessing.Run(assemblyDefinition);
                wasModified |= functionPointerProcessing.Run(assemblyDefinition);
                if (wasModified)
                {
                    var peStream = new MemoryStream();
                    var pdbStream = new MemoryStream();
                    var writeParameters = new WriterParameters
                    {
                        SymbolWriterProvider = new PortablePdbWriterProvider(),
                        WriteSymbols = true,
                        SymbolStream = pdbStream
                    };

                    assemblyDefinition.Write(peStream, writeParameters);
                    peStream.Flush();
                    pdbStream.Flush();

                    peData = peStream.ToArray();
                    pdbData = pdbStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Internal compiler error for Burst ILPostProcessor on {compiledAssembly.Name}. Exception: {ex}");
            }

            if (debugging)
            {
                Log($"End processing assembly {compiledAssembly.Name} in {clock.Elapsed.TotalMilliseconds}ms.");
            }

            if (wasModified && !diagnostics.Any(d => d.DiagnosticType == DiagnosticType.Error))
            {
                return new ILPostProcessResult(new InMemoryAssembly(peData, pdbData), diagnostics);
            }
            return new ILPostProcessResult(null, diagnostics);
        }

        private static void Log(string message)
        {
#if !UNITY_2020_2_OR_NEWER && !UNITY_DOTSPLAYER
            UnityEngine.Debug.Log($"{nameof(BurstILPostProcessor)}: {message}");
#else
            Console.WriteLine($"{nameof(BurstILPostProcessor)}: {message}");
#endif
        }

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(f => Path.GetFileName(f) == "Unity.Burst.dll");
        }
    }
}
#endif
