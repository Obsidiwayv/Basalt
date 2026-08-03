using Basalt.BackendPipes.Compression;
using Basalt.BackendPipes.MSVC;
using Basalt.LavaLang;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Basalt.BackendPipes.LLVM
{
    public class WindowsLLVMInstance : ILLVMInstance
    {
        private MSVCInstallation MSVCDirectory { get; }

        private SharedLLVMInstance Shared { get; }

        public WindowsLLVMInstance(SharedLLVMInstance Shared)
        {
            BasaltProject SharedProject = Shared.Project.ParentProject ?? Shared.Project;

            // Invoking the msvc function to get a version the build tool should look for
            // This is only used for copying redist files
            LavaFunctionNode? MSVCVersionFunction = (LavaFunctionNode?)SharedProject.GetNode("SetMSVCVersion");

            string? MSVCVersion = null;
            if (MSVCVersionFunction != null)
            {
                MSVCVersion = MSVCVersionFunction.Invoke<string>();
            }

            MSVCDirectory = MSVCUtil.GetVersionedDirectory(MSVCVersion);
            this.Shared = Shared;
        }

        public void RunExecutableTask()
        {
            List<string> DebugFlags = SharedLLVMInstance.GetCompilerDebugFlags();
            List<string> Objects = Shared.CompileSourcesToObjects(Shared.ReleaseType);

            string ExecutableName = Path.Join(Shared.OutputDirectory, Shared.GetExecutableName());

            List<string> OSFlags = [];

            LavaStringNode? ResourceFile = (LavaStringNode?)Shared.Project.GetNode("RCFile");
            if (ResourceFile != null)
            {
                string OutputDir = "Bin/Resource";
                string OutputRes = Path.Join(OutputDir, $"{Path.GetFileNameWithoutExtension(ResourceFile.Value)}.res");

                Directory.CreateDirectory(OutputDir);
                BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetLLVMExecutableFromBin("llvm-rc"),
                    [ResourceFile.Value, "/FO", OutputRes]);

                OSFlags.Add(OutputRes);
            }
            CheckDebugSymbolsMacro(OSFlags);

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                [$"-o {ExecutableName}",
                "-Wl,/NOIMPLIB",
                ..DebugFlags,
                ..OSFlags,
                ..Shared.RequiredLibraryLinkFiles,
                ..Shared.ThirdPartyLibraries.LinkFiles,
                ..BasicProvider.GetOSFlags(Shared.Project, "Flags"),
                string.Join(" ", Objects)]);

            CopyPDB();
            BasaltGlobalFileCache.PushFile(ExecutableName);
        }

        public void RunDynamicLibraryTask()
        {
            List<string> DebugFlags = SharedLLVMInstance.GetCompilerDebugFlags();
            List<string> Objects = Shared.CompileSourcesToObjects(Shared.ReleaseType);
            List<string> ExtraFlags = [];

            string Arch = BasicProvider.GetOSArch();
            string LibraryName = $"{Shared.ProjectName.Value}-x{Arch}";
            List<string> Includes = Shared.GetIncludes();

            string LibraryLinkFile = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}.lib";
            ExtraFlags.Add($"-Wl,/IMPLIB:{LibraryLinkFile}");

            BasaltGlobalFileCache.LibraryCache.Add(
                new(
                    Shared.ProjectName.Value,
                    ELibraryType.Dynamic,
                    Shared.Project.FileSource.Name, LibraryLinkFile, Includes));
            CheckDebugSymbolsMacro(ExtraFlags);

            string BinaryName = $"{Path.Join(Shared.OutputDirectory, LibraryName)}.dll";
            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                [$"-o {BinaryName}",
                        "-shared",
                        ..ExtraFlags,
                        ..DebugFlags,
                        ..Shared.ThirdPartyLibraries.LinkFiles,
                        ..BasicProvider.GetOSFlags(Shared.Project, "Flags"),
                        string.Join(" ", Objects)]);

            CopyPDB();
            BasaltGlobalFileCache.PushFile(BinaryName);
        }

        public void RunStaticLibraryTask()
        {
            List<string> Objects = Shared.CompileSourcesToObjects(Shared.ReleaseType);

            string Arch = BasicProvider.GetOSArch();
            string LibraryName = $"{Shared.ProjectName.Value}-x{Arch}";
            List<string> Includes = Shared.GetIncludes();

            string LibraryOutputName = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}-static.lib";

            List<string> UnixFlags = [];
            if (!OperatingSystem.IsWindows())
            {
                UnixFlags.Add(LibraryOutputName);
            }

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetLLVMExecutableFromBin("llvm-lib"), [
                $"/OUT:{LibraryOutputName}",
                ..UnixFlags,
                ..Objects]);
            BasaltGlobalFileCache.LibraryCache.Add(
                new(
                    Shared.ProjectName.Value,
                    ELibraryType.Static,
                    Shared.Project.FileSource.Name,
                    LibraryOutputName,
                    Includes));
        }

        public void HandleCachedLibrary(BasaltLibraryCache CachedLibrary)
        {
            Shared.RequiredLibraryLinkFiles.Add(CachedLibrary.OutputFile);
        }

        public void HandleFinish()
        {
            LavaArrayNode? AssetArrayNode = (LavaArrayNode?)
                Shared.Project.GetNode("Assets");

            if (AssetArrayNode != null)
            {
                BasaltAssetsPipeline Pipeline = new(true);
                string ResourcesDir = BasicProvider.GetDebugOrReleaseDir();

                foreach (string Asset in AssetArrayNode.Value)
                {
                    if (File.Exists(Asset))
                    {
                        File.Copy(Asset, Path.Join(ResourcesDir, Asset), true);
                        continue;
                    }
                    Pipeline.CopyFiles(Asset, ResourcesDir);
                }
            }
        }

        public void CheckDebugSymbolsMacro(List<string> FlagsArray)
        {
            string PDBFile = Path.Join(Shared.OutputDirectory, $"{Shared.GetExecutableName(false)}.pdb");
            bool DebugSymbolsDisabled = Shared.Project.MacroIsPresent("DisableDSYM");

            if (DebugSymbolsDisabled)
            {
                // The macro was specified so we cant have pdb files on this build
                // If another debug symbols file is present, delete it
                if (File.Exists(PDBFile)) File.Delete(PDBFile);
                FlagsArray.Add("-Wl,/DEBUG:NONE");
            }

            if (SharedLLVMInstance.ReleaseMode && !DebugSymbolsDisabled)
            {
                FlagsArray.Add("-g");
            }
        }

        public void CopyPDB()
        {
            string PDBFile = Path.Join(Shared.OutputDirectory, $"{Shared.GetExecutableName(false)}.pdb");
            if (File.Exists(PDBFile))
            {
                File.Copy(PDBFile, Path.Join("Bin", Path.GetFileName(PDBFile)), true);

                string ID = BasaltBuildId.Generate(false);
                File.Copy(PDBFile, Path.Join(
                    BasaltDirectoryTiles.Symbols.Value, $"{Path.GetFileNameWithoutExtension(PDBFile)}-{ID}.pdb"));
            }
        }
    }
}
