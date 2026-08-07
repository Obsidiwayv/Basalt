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
using System.Runtime.InteropServices;
using System.Text;

namespace Basalt.BackendPipes.LLVM
{
    public class WindowsLLVMInstance : ILLVMInstance
    {
        private string MSVCDirectory { get; }

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
            List<string> DebugFlags = Shared.GetCompilerDebugFlags();
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
                ..Shared.GetOSFlags(Shared.Project, "Flags"),
                string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(ExecutableName);
        }

        public void RunDynamicLibraryTask()
        {
            List<string> DebugFlags = Shared.GetCompilerDebugFlags();
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
                        ..Shared.GetOSFlags(Shared.Project, "Flags"),
                        string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(BinaryName);
        }

        public void RunStaticLibraryTask()
        {
            List<string> Objects = Shared.CompileSourcesToObjects(Shared.ReleaseType);

            string Arch = BasicProvider.GetOSArch();
            string LibraryName = $"{Shared.ProjectName.Value}-x{Arch}";
            List<string> Includes = Shared.GetIncludes();

            string LibraryOutputName = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}-static.lib";

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetLLVMExecutableFromBin("llvm-lib"), [
                $"/OUT:{LibraryOutputName}",
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
            // asset handling
            LavaArrayNode? AssetArrayNode = (LavaArrayNode?)
                Shared.Project.GetNode("Assets");

            if (AssetArrayNode != null)
            {
                string ResourcesDir = BasicProvider.GetDebugOrReleaseDir();

                foreach (string Asset in AssetArrayNode.Value)
                {
                    if (File.Exists(Asset))
                    {
                        File.Copy(Asset, Path.Join(ResourcesDir, Asset), true);
                        continue;
                    }
                    BasaltAssetsPipeline.CopyFiles(Asset, ResourcesDir);
                }
            }

            // Redist handling
            string RedistDir = Path.Join(SharedLLVMInstance.GetDebugOrReleaseDir(), "_Redist");
            string Redist = Directory.GetDirectories(
                Path.Join(MSVCDirectory, "VC", "Redist", "MSVC"), "*", SearchOption.TopDirectoryOnly)[0];

            Directory.CreateDirectory(RedistDir);

            if (RuntimeInformation.OSArchitecture == Architecture.X64
                || RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                Redist = Path.Join(Redist, "vc_redist.x64.exe");
            }
            if (RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                Redist = Path.Join(Redist, "vc_redist.x86.exe");
            }
            File.Copy(Redist, Path.Join(RedistDir, Path.GetFileName(Redist)), true);
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
    }
}
