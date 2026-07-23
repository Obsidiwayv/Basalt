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
    // This code was getting harder to maintain properly
    // so it was split it up into os specifc llvm classes
    [Obsolete("Replaced with LLVMProvider")]
    public class LlvmProvider : BasicProvider, IProvider
    {
        public FUseCompiler CompilerMetadata { get; }
        private List<string> RequiredLibraryIncludes { get; } = [];
        private List<string> RequiredLibraryLinkFiles { get; } = [];
        private BasaltThirdPartyLibraries ThirdPartyLibraries = new();

        public LlvmProvider(BasaltProject Project,
            FUseCompiler CompilerMetadata,
            string[] Args) : base(Project, CompilerMetadata.CompilerName, Args, true)
        {
            this.CompilerMetadata = CompilerMetadata;

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
                        if (OperatingSystem.IsWindows())
                        {
                            RequiredLibraryLinkFiles.Add(CachedLibrary.OutputFile);
                        }
                        else
                        {
                            if (CachedLibrary.LibType == ELibraryType.Static)
                            {
                                RequiredLibraryLinkFiles.Add(CachedLibrary.OutputFile);
                            } else
                            {
                                RequiredLibraryLinkFiles.Add($"-l{CachedLibrary.Name}");
                            }
                        }
                        BasaltLogger.WriteLine(CachedLibrary.Name);
                        if (OperatingSystem.IsMacOS()
                            && CachedLibrary.LibType == ELibraryType.Dynamic
                            && CompilerMode == EBinaryType.Executable)
                        {
                            string LibFileName = $"lib{CachedLibrary.Name}-x{GetOSArch()}.dylib";
                            File.Move(
                                Path.Join(
                                   GetDebugOrReleaseDir(),
                                   LibFileName),
                                Path.Join(
                                    BasaltMacAppPackage.GetOrCreatePackageFolder(this, "Frameworks"),
                                    LibFileName), true);
                        }
                    }
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
                    BasaltLanguageParser Parser = new BasaltLanguageLexer(new(LavaFile))
                        .Run()
                        .PipeIntoParser(null)
                        .Run();
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
                    if (OperatingSystem.IsMacOS() && F.EndsWith(".a"))
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
            foreach (string SourceFile in SourcesNode.Value)
            {
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

                string CompilerPath = GetClangExecutableCommand(bIsCFile);
                BasicCompilerBackend.ExecuteTool(CompilerPath, Args);

                BasaltCompilationDatabase.PushEntry(
                    Directory.GetCurrentDirectory(), [CompilerPath, .. Args], SourceFile);

                ObjectFilePaths.Add(SourceNameWithObject);
            }

            return ObjectFilePaths;
        }

        public void BuildExecutableTask()
        {
            SetCompilerMode(EBinaryType.Executable, null);
            List<string> DebugFlags = GetCompilerDebugFlags();
            List<string> Objects = CompileToObjects();
            string ExecutableName;
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                ExecutableName = Path.Join(OutputDirectory, GetExecutableName());
            }
            else
            {
                string MacPackageOutput = BasaltMacAppPackage
                    .GetOrCreatePackageFolder(this, "MacOS");

                ExecutableName = Path.Join(
                    MacPackageOutput,
                    GetExecutableName());
            }

            List<string> OSFlags = [];

            if (OperatingSystem.IsWindows())
            {
                OSFlags.Add("-Wl,/NOIMPLIB");

                LavaStringNode? ResourceFile = (LavaStringNode?)Project.GetNode("RCFile");
                if (ResourceFile != null)
                {
                    string OutputDir = "Bin/Resource";
                    string OutputRes = Path.Join(OutputDir, $"{Path.GetFileNameWithoutExtension(ResourceFile.Value)}.res");
                    Directory.CreateDirectory(OutputDir);
                    BasicCompilerBackend.ExecuteTool(GetLLVMExecutableFromBin("llvm-rc"),
                        [ResourceFile.Value, "/FO", OutputRes]);

                    OSFlags.Add(OutputRes);
                }
            }
            if (OperatingSystem.IsMacOS())
            {
                string OutputFrameworkPath = BasaltMacAppPackage
                        .GetOrCreatePackageFolder(this, "Frameworks");
                if (Directory.Exists(OutputFrameworkPath))
                {
                    OSFlags.Add("-Wl,-rpath,@executable_path/../Frameworks");
                }
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
                            new(
                                ProjectName.Value,
                                ELibraryType.Dynamic,
                                Project.FileSource.Name, LibraryLinkFile, Includes));
                    }
                    string BinaryName = $"{Path.Join(OutputDirectory, LibraryName)}.{GetLibFileExtension()}";
                    if (OperatingSystem.IsMacOS())
                    {
                        string FolderOutput = Path.Join(
                            GetDebugOrReleaseDir(),
                            $"lib{LibraryName}");

                        BinaryName = $"{FolderOutput}.{GetLibFileExtension()}";
                        BasaltGlobalFileCache.LibraryCache.Add(
                           new(
                               ProjectName.Value,
                               ELibraryType.Dynamic,
                               Project.FileSource.Name, BinaryName, Includes));
                    }
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
                    string UnixOrWinLib = OperatingSystem.IsWindows() ? ".lib" : ".a";
                    string LibraryOutputName = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}-static{UnixOrWinLib}";

                    string ArchiverTool = OperatingSystem.IsWindows()
                        ? GetLLVMExecutableFromBin("llvm-lib") : "ar";
                    string OutputArgument = OperatingSystem.IsWindows()
                        ? $"/OUT:{LibraryOutputName}"
                        : "rcs";

                    List<string> UnixFlags = [];
                    if (!OperatingSystem.IsWindows())
                    {
                        UnixFlags.Add(LibraryOutputName);
                    }

                    BasicCompilerBackend.ExecuteTool(ArchiverTool, [
                        OutputArgument,
                        ..UnixFlags,
                        ..Objects]);
                    BasaltGlobalFileCache.LibraryCache.Add(
                        new(
                            ProjectName.Value,
                            ELibraryType.Static,
                            Project.FileSource.Name,
                            LibraryOutputName,
                            Includes));
                    break;
            }
        }
    }
}
