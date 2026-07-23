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
        

        public void Finish(BasaltProject Project)
        {
            throw new NotImplementedException();
        }
    }
}