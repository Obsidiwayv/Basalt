using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Basalt.BackendPipes.LLVM
{

    public interface ILLVMInstance
    {
        public void RunExecutableTask();
        public void RunStaticLibraryTask();
        public void RunDynamicLibraryTask();
        public void HandleCachedLibrary(BasaltLibraryCache CachedLibrary);
        public void HandleFinish();
    }

    public class SharedLLVMInstance : BasicProvider
    {
        public FUseCompiler CompilerMetadata { get; }
        public List<string> RequiredLibraryIncludes { get; } = [];
        public List<string> RequiredLibraryLinkFiles { get; } = [];
        public List<string> BuildFlags { get; } = [];

        public BasaltThirdPartyLibraries ThirdPartyLibraries = new();

        public SharedLLVMInstance(BasaltProject Project,
            FUseCompiler CompilerMetadata,
            string[] Args) : base(Project, CompilerMetadata.CompilerName, Args, true)
        {
            this.CompilerMetadata = CompilerMetadata;

            // @ThirdParty "Libs"
            LavaStringNode? ThirdPartyLibFolder = (LavaStringNode?)Project.GetNode("ThirdParty");
            if (ThirdPartyLibFolder != null)
            {
                if (!Directory.Exists(ThirdPartyLibFolder.Value))
                    throw new BasaltException($"%r{ThirdPartyLibFolder}%c is not a valid library folder!");

                foreach (string LavaFile in Directory.EnumerateFiles(
                    ThirdPartyLibFolder.Value, $"*{BasaltLavaFile.Extension}", SearchOption.AllDirectories))
                {
                    BasaltLanguageParser Parser = new BasaltLanguageLexer(new(LavaFile))
                        .Run()
                        .PipeIntoParser(null) // Third-Party Libraries dont need a parent
                        .Run();
                    HandleThirdPartyLibrary(Parser.Project);
                }
            }

            BasaltLogger.WriteLine($"Starting Compilation of %m{ProjectName.Value}%c...");
        }

        public static string GetLLVMExecutableFromBin(string ExecutableName)
        {
            StringBuilder ClangPath = new();
            if (OperatingSystem.IsWindows())
            {
                ClangPath.Append(@"C:/Program Files/LLVM/bin/");
            }
            ClangPath.Append(ExecutableName);
            return OperatingSystem.IsWindows() ? $"{ClangPath}.exe" : ClangPath.ToString();
        }

        public static string GetClangExecutableCommand(bool UsingC)
        {
            return GetLLVMExecutableFromBin(
                UsingC ? "clang" : "clang++");
        }

        public List<string> GetCompilerDebugFlags()
        {
            List<string> DebugFlags = [];
            if ((DebugMode || PreviewMode) && !Project.MacroIsPresent("DisableDSYM"))
            {
                DebugFlags.Add("-g3");
            }
            if (!DebugMode) return DebugFlags;
            DebugFlags.Add("-O0");
            DebugFlags.Add("-fsanitize=undefined");
            DebugFlags.Add("-fno-omit-frame-pointer");
            DebugFlags.Add("-fstandalone-debug");
            return DebugFlags;
        }

        private void HandleThirdPartyLibrary(BasaltProject Lib)
        {
            LavaArrayNode? Headers = (LavaArrayNode?)Lib.GetNode("Includes");
            LavaArrayNode? LibFiles = (LavaArrayNode?)Lib.GetNode("Link");
            LavaArrayNode? Sources = (LavaArrayNode?)Lib.GetNode("Sources");
            LavaStringNode LibName = (LavaStringNode?)Lib.GetNode("Name")
                ?? throw new BasaltException("Unknown Library!");

            List<string> SecondaryLibFiles = [];

            LavaArrayNode? Resources = null;
            // This is a windows only thing
            if (OperatingSystem.IsWindows())
            {
                Resources = (LavaArrayNode?)Lib.GetNode("ResourcesWin32");
            }
            if (OperatingSystem.IsMacOS())
            {
                Resources = (LavaArrayNode?)Lib.GetNode("ResourcesMac");
            }

            if (Headers != null) foreach (string IncludeDir in Headers.Value)
                ThirdPartyLibraries.Headers.Add($"-I{IncludeDir}");

            if (LibFiles != null)
            {
                SecondaryLibFiles = LibFiles.Value;
                if (Resources != null && OperatingSystem.IsMacOS())
                {
                    SecondaryLibFiles = [..LibFiles.Value, ..Resources.Value];
                }
                foreach (string F in SecondaryLibFiles)
                {
                    if (OperatingSystem.IsWindows() && F.EndsWith(".lib"))
                        ThirdPartyLibraries.LinkFiles.Add(F);
                    if (OperatingSystem.IsMacOS() 
                            && F.EndsWith(".a") || F.EndsWith(".dylib"))
                        ThirdPartyLibraries.LinkFiles.Add(F);
                }
            }

            if (Resources != null)
            {
                foreach (string Resource in Resources.Value)
                {
                    if (File.Exists(Resource))
                    {
                        string FileName = Path.GetFileName(Resource);
                        File.Copy(Resource, Path.Join(GetDebugOrReleaseDir(), FileName), true);
                    }
                    else
                    {
                        throw new BasaltException($"%r{Resource}$c is an invalid resource");
                    }
                }
            }

            if (Sources != null)
                ThirdPartyLibraries.Sources.AddRange(Sources.Value);
        }

        public List<string> GetIncludes()
        {
            List<string> IncludeList = [];
            LavaArrayNode? IncludeDirs = (LavaArrayNode?)Project.GetNode("Includes");

            if (IncludeDirs != null)
            {
                foreach (string IncludePath in IncludeDirs.Value)
                {
                    // The user defined it as a system header
                    if (IncludePath.EndsWith(",system"))
                    {
                        string[] SplitPath = IncludePath.Split(",system");
                        IncludeList.Add($"-isystem{SplitPath[0]}");
                        continue;
                    }
                    IncludeList.Add($"-I{IncludePath}");
                }
                return IncludeList;
            }
            return [];
        }

        public List<string> CompileSourcesToObjects(EReleaseMode ReleaseMode = EReleaseMode.Shipping)
        {
            LavaArrayNode SourcesNode = (LavaArrayNode?)Project.GetNode("Sources")
                ?? throw new BasaltException("Project cannot be compiled: Missing Sources Array");

            List<string> ObjectFilePaths = [];
            List<string> ExtraFlags = [];

            string OutputPath = Path.Combine(
                BasaltDirectoryTiles.Object.Value,
                ProjectName.Value);

            if (Directory.Exists(OutputPath))
            {
                // Resetting the object file cache path
                Directory.Delete(OutputPath, true);
            }
            Directory.CreateDirectory(OutputPath);

            if (ThirdPartyLibraries.Sources.Count != 0)
            {
                SourcesNode.Value.AddRange(ThirdPartyLibraries.Sources);
            }
            if (ReleaseMode == EReleaseMode.Shipping && !Project.MacroIsPresent("DisableDSYM"))
            {
                ExtraFlags.Add("-g");
            }
            foreach (string SourceFile in SourcesNode.Value)
            {
                // Its a directory
                if (Directory.Exists(SourceFile))
                {
                    foreach (string SubSourceFile in Directory.EnumerateFiles(
                        ParseStringVariables(SourceFile), "*", SearchOption.AllDirectories))
                    {
                        CompileSource(SubSourceFile, ExtraFlags, OutputPath, ObjectFilePaths);
                    }
                    continue;
                }
                CompileSource(SourceFile, ExtraFlags, OutputPath, ObjectFilePaths);
            }
            return ObjectFilePaths;
        }

        private void CompileSource(string SourceFile, List<string> Flags, string Output, List<string> OBJArray)
        {
            string SourceNameWithObject = Path.Combine(
                Output,
                $"{Path.GetFileNameWithoutExtension(SourceFile)}.{(OperatingSystem.IsWindows() ? "obj" : "o")}")
                .Replace("\\", "/");

            List<string> Includes = GetIncludes();

            List<string> Args = [
                "-c", 
                SourceFile,
                $"-o {SourceNameWithObject}",
                ..Flags,
                ..Includes,
                ..ThirdPartyLibraries.Headers,
                ..RequiredLibraryIncludes, ..GetOSFlags(Project, "CompileFlags")];

            bool bIsCFile = SourceFile.EndsWith(".c");
            if (!bIsCFile)
            {
                BasaltDefaults.CPPVersions.TryGetValue(CompilerMetadata.LanguageVersion, out string? Version);
                // If the version doesnt exist default to the build tools default 
                Version ??= $"c++{BasaltDefaults.CPP}";
                Args.Add($"-std={Version}");
            }
            else
            {
                LavaFunctionNode? LanguageUseC = (LavaFunctionNode?)Project.GetNode("CLangVersion");

                int LangVersion = BasaltDefaults.C;
                if (LanguageUseC != null)
                {
                    LangVersion = LanguageUseC.Invoke<int>();
                }
                BasaltDefaults.CVersions.TryGetValue(LangVersion, out string? Version);
                Args.Add($"-std={Version}");
            }

            string CompilerPath = GetClangExecutableCommand(bIsCFile);
            BasaltLogger.WriteLine($"%m{SourceFile}%c >>> %m{SourceNameWithObject}%c");
            BasicCompilerBackend.ExecuteTool(CompilerPath, Args);

            BasaltCompilationDatabase.PushEntry(
                Directory.GetCurrentDirectory(), [CompilerPath, .. Args], SourceFile);

            OBJArray.Add(SourceNameWithObject);
        }
    }
}
