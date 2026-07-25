using System.IO.Compression;
using Basalt.LavaLang;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;

namespace Basalt.BackendPipes.LLVM
{
    public class LLVMProvider : IProvider
    {
        public FUseCompiler CompilerMetadata { get; }
        public ILLVMInstance Instance { get; }
        public SharedLLVMInstance SharedInstance { get; }

        public LLVMProvider(
            BasaltProject Project,
            FUseCompiler CompilerMetadata,
            string[] Args)
        {
            SharedInstance = new(Project, CompilerMetadata, Args);

            Instance = BasicCompilerBackend.GetOSEnum() switch
            {
                OSInformation.MacOS => new MacLLVMInstance(SharedInstance),
                _ => throw new BasaltException("Compiler does not support your operating system"),
            };

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
                        SharedInstance.RequiredLibraryIncludes.Add(string.Join(" ", CachedLibrary.Includes));
                        Instance.HandleCachedLibrary(CachedLibrary);
                    }
                }
            }

            this.CompilerMetadata = CompilerMetadata;
        }

        public void BuildExecutableTask()
        {
            SharedInstance.SetCompilerMode(EBinaryType.Executable, null);
            Instance.RunExecutableTask();
        }

        public void BuildLibraryTask(ELibraryType LibraryType)
        {
            SharedInstance.SetCompilerMode(EBinaryType.Library, LibraryType);

            switch (LibraryType)
            {
                case ELibraryType.Static:
                    Instance.RunStaticLibraryTask();
                    break;
                case ELibraryType.Dynamic:
                    Instance.RunDynamicLibraryTask();
                    break;
            }
        }

        public void Finish(BasaltProject Project, string ProjectName)
        {
            FinishAssets();
            Instance.HandleFinish();
            if (SharedInstance.CompilerMode != EBinaryType.Executable) return;

            string AppFolderName = BasaltMacAppPackage.GetAppFolderFromName(
                SharedInstance, ProjectName);

            if (OperatingSystem.IsMacOS()
                && Directory.Exists(AppFolderName))
            {
                foreach (string DylibFile in Directory.EnumerateFiles(
                    SharedInstance.GetDebugOrReleaseDir(), "*.dylib", SearchOption.TopDirectoryOnly))
                {
                    File.Move(
                        DylibFile,
                        Path.Join(
                            BasaltMacAppPackage
                                .GetOrCreateNamedPackageFolder(SharedInstance, $"{ProjectName}.app", "Frameworks"),
                            Path.GetFileName(DylibFile)), true);
                }
            }

            if (SharedInstance.PackageIntoZip)
            {
                string FilePath = Path.Join("Bin", $"{SharedInstance.GetExecutableName(false)}.zip");

                // Delete the old zip file since dotnet doesnt do it automcatically
                if (File.Exists(FilePath)) File.Delete(FilePath);

                ZipFile.CreateFromDirectory(
                    SharedInstance.GetDebugOrReleaseDir(),
                    FilePath,
                    CompressionLevel.Optimal, false
                );
            }
        }

        private void FinishAssets()
        {
            if (SharedInstance.DepthLogging)
                BasaltLogger.WriteLine($"%bFinished Layer {BasaltGlobalStats.Depth}%c");

            BasaltGlobalFileCache.WriteIntoCache();
        }
    }
}