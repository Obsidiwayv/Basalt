using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Basalt.BackendPipes.LLVM
{
    public class LlvmProvider : BasicProvider, IProvider
    {
        public FUseCompiler CompilerMetadata { get; }

        private BasaltProject Project { get; }

        private LavaStringNode ProjectName { get; }

        private List<string> RequiredLibraryIncludes { get; } = [];
        private List<string> RequiredLibraryLinkFiles { get; } = [];
        private BasaltThirdPartyLibraries ThirdPartyLibraries = new();

        public LlvmProvider(BasaltProject Project,
            FUseCompiler CompilerMetadata,
            string[] Args) : base(CompilerMetadata.CompilerName, Args, true)
        {
            this.CompilerMetadata = CompilerMetadata;
            this.Project = Project;

            ProjectName = (LavaStringNode?)Project.GetNode("name")
                ?? throw new BasaltException("Project is missing a name attribute!");

            LavaArrayNode? Dependencies = (LavaArrayNode?)Project.GetNode("Using");
            if (Dependencies != null)
            {
                foreach (string ProjectFileDependency in Dependencies.Value)
                {
                    BasaltLibraryCache? CachedLibrary = BasaltGlobalFileCache
                        .LibraryCache.Find(Lib => Lib.ProjectFile == ProjectFileDependency);

                    // Take the library file we cached earlier and use that to link to the final executable
                    if (CachedLibrary != null) 
                    {
                        RequiredLibraryIncludes.Add(string.Join(" ", CachedLibrary.Includes));
                        RequiredLibraryLinkFiles.Add(CachedLibrary.OutputFile);
                    };
                }
            }

            // @ThirdParty "Libs"
            LavaStringNode? ThirdPartyLibFolder = (LavaStringNode?)Project.GetNode("ThirdParty");
            if (ThirdPartyLibFolder != null)
            {
                if (!Directory.Exists(ThirdPartyLibFolder.Value))
                    throw new BasaltException($"%r{ThirdPartyLibFolder}%c is not a valid library folder!");

                foreach (string LavaFile in Directory.EnumerateFiles(
                    ThirdPartyLibFolder.Value, $"*{BasaltLavaFile.Extension}", SearchOption.AllDirectories))
                {
                    BasaltLanguageLexer Lexer = new(new(LavaFile));
                    Lexer.Run();
                    BasaltLanguageParser Parser = Lexer.UseParser();
                    Parser.Run();
                    HandleThirdPartyLibrary(Parser.Project);
                }
            }

            BasaltLogger.WriteLine($"Starting Compilation of %m{ProjectName.Value}%c...");

            LavaFunctionNode? LanguageUseC = (LavaFunctionNode?)Project.GetNode("UseC");
            LanguageUseC?.Invoke<bool>();
        }

        private static string GetLLVMExecutableFromBin(string ExecutableName)
        {
            StringBuilder ClangPath = new();
            if (OperatingSystem.IsWindows())
            {
                ClangPath.Append(@"C:/Program Files/LLVM/bin/");
            }
            ClangPath.Append(ExecutableName);
            return OperatingSystem.IsWindows() ? $"{ClangPath}.exe" : ClangPath.ToString();
        }

        private static string GetClangExecutableCommand(bool UsingC)
        {
            return GetLLVMExecutableFromBin(
                UsingC ? "clang" : "clang++");
        }

        private List<string> GetCompilerDebugFlags()
        {
            if (!DebugMode) return [];
            List<string> DebugFlags = [];
            DebugFlags.Add("-g3");
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

            LavaArrayNode? Resources = null;
            if (OperatingSystem.IsWindows())
            {
                Resources = (LavaArrayNode?)Lib.GetNode("ResourcesWin32");
            }

            if (Headers != null) foreach (string IncludeDir in Headers.Value) 
                ThirdPartyLibraries.Headers.Add($"-I{IncludeDir}");

            if (LibFiles != null)
            {
                foreach (string F in LibFiles.Value)
                {
                    if (OperatingSystem.IsWindows() && F.EndsWith(".lib")) 
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
                    } else
                    {
                        throw new BasaltException($"%r{Resource}$c is an invalid resource");
                    }
                }
            }

            if (Sources != null)
                ThirdPartyLibraries.Sources.AddRange(Sources.Value);
        }

        private List<string> GetIncludes()
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

        private List<string> CompileToObjects()
        {
            LavaArrayNode SourcesNode = (LavaArrayNode?)Project.GetNode("Sources")
                ?? throw new BasaltException("Project cannot be compiled: Missing Sources Array");

            List<string> ObjectFilePaths = [];

            string OutputPath = Path.Combine(
                BasaltDirectoryTiles.Object.Value,
                ProjectName.Value);

            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                // Resetting the object file cache path
                Directory.Delete(OutputPath, true);
                Directory.CreateDirectory(OutputPath);
            }
            if (ThirdPartyLibraries.Sources.Count != 0)
            {
                SourcesNode.Value.AddRange(ThirdPartyLibraries.Sources);
            }
            foreach (string SourceFile in SourcesNode.Value)
            {
                Console.WriteLine("\n\n");
                Console.WriteLine(SourceFile);
                string SourceNameWithObject = Path.Combine(
                    OutputPath,
                    $"{Path.GetFileNameWithoutExtension(SourceFile)}.{(OperatingSystem.IsWindows() ? "obj" : "o")}")
                    .Replace("\\", "/");

                List<string> Includes = GetIncludes();

                List<string> Args = [
                    "-c", SourceFile, 
                    $"-o {SourceNameWithObject}",
                    ..Includes,
                    ..ThirdPartyLibraries.Headers,
                    ..RequiredLibraryIncludes, ..BasicCompilerBackend.GetOSFlags(Project.Nodes)];

                bool bIsCFile = SourceFile.EndsWith(".c");
                if (!bIsCFile)
                {
                    BasaltDefaults.CPPVersions.TryGetValue(CompilerMetadata.LanguageVersion, out string? Version);
                    // If the version doesnt exist default to the build tools default 
                    Version ??= $"c++{BasaltDefaults.CPP}";
                    Args.Add($"-std={Version}");
                }

                List<string> ArgsWithCompiler = [GetClangExecutableCommand(bIsCFile), .. Args];
                BasicCompilerBackend.ExecuteTool(GetClangExecutableCommand(bIsCFile), Args);

                BasaltCompilationDatabase.PushEntry(Directory.GetCurrentDirectory(), ArgsWithCompiler, SourceFile);

                ObjectFilePaths.Add(SourceNameWithObject);
            }

            return ObjectFilePaths;
        }

        public void BuildExecutableTask()
        {
            SetCompilerMode(EBinaryType.Executable, null);

            string DirectoryByFlag = GetDebugOrReleaseDir();
            List<string> DebugFlags = GetCompilerDebugFlags();
            List<string> Objects = CompileToObjects();
            string ExecutableName = Path.Join(DirectoryByFlag, GetExecutableName(ProjectName.Value));

            List<string> OSFlags = [];

            if (OperatingSystem.IsWindows())
            {
                OSFlags.Add("-Wl,/NOIMPLIB");
            }

            BasicCompilerBackend.ExecuteTool(GetClangExecutableCommand(false),
                [$"-o {ExecutableName}",
                ..DebugFlags,
                ..OSFlags,
                ..RequiredLibraryLinkFiles,
                ..ThirdPartyLibraries.LinkFiles,
                string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(ExecutableName);
        }

        public void BuildLibraryTask(ELibraryType LibraryType)
        {
            SetCompilerMode(EBinaryType.Library, LibraryType);

            string DirectoryByFlag = GetDebugOrReleaseDir();
            List<string> DebugFlags = GetCompilerDebugFlags();
            List<string> Objects = CompileToObjects();

            string Arch = GetOSArch();
            string LibraryName = $"{ProjectName.Value}-x{Arch}";
            List<string> Includes = GetIncludes();

            switch (LibraryType)
            {
                case ELibraryType.Dynamic:
                    List<string> ExtraFlags = [];

                    if (OperatingSystem.IsWindows())
                    {
                        string LibraryLinkFile = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}.lib";
                        ExtraFlags.Add($"-Wl,/IMPLIB:{LibraryLinkFile}");

                        BasaltGlobalFileCache.LibraryCache.Add(
                            new(Project.FileSource.Name, LibraryLinkFile, Includes));
                    }
                    string BinaryName = $"{Path.Join(DirectoryByFlag, LibraryName)}.{GetLibFileExtension()}";
                    BasicCompilerBackend.ExecuteTool(GetClangExecutableCommand(false),
                        [$"-o {BinaryName}",
                        OperatingSystem.IsMacOS() ? "-dynamiclib" : "-shared",
                        ..ExtraFlags,
                        ..DebugFlags,
                        ..ThirdPartyLibraries.LinkFiles,
                        string.Join(" ", Objects)]);

                    BasaltGlobalFileCache.PushFile(BinaryName);
                    break;
                case ELibraryType.Static:
                    if (OperatingSystem.IsWindows())
                    {
                        string LibraryOutputName = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}-static.lib";
                        BasicCompilerBackend.ExecuteTool(GetLLVMExecutableFromBin("llvm-lib"), [
                            $"/OUT:{LibraryOutputName}",
                            ..Objects]);
                        BasaltGlobalFileCache.LibraryCache.Add(
                            new(Project.FileSource.Name, LibraryOutputName, Includes));
                    }
                    break;
            }
        }
    }
}
