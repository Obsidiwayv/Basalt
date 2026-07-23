using Basalt.BackendPipes.LLVM;
using Basalt.LavaLang;
using Basalt.LavaLang.Functions;

namespace Basalt.BackendPipes.LLVM
{
    public class MacLLVMInstance(
        SharedLLVMInstance Shared) : ILLVMInstance
    {

        private static string LibraryFile(string Name) =>
            $"lib{Name}-x{BasicProvider.GetOSArch()}.dylib";

        public void RunExecutableTask()
        {
            List<string> DebugFlags = Shared.GetCompilerDebugFlags();
            List<string> Objects = Shared.CompileSourcesToObjects();
            string MacPackageOutput = BasaltMacAppPackage
                .GetOrCreatePackageFolder(Shared, "MacOS");

            string ExecutableName = Path.Join(
                MacPackageOutput,
                Shared.GetExecutableName());

            List<string> OSFlags = [];
            string OutputFrameworkPath = BasaltMacAppPackage
                    .GetOrCreatePackageFolder(Shared, "Frameworks");
            if (Directory.Exists(OutputFrameworkPath))
            {
                OSFlags.Add("-Wl,-rpath,@executable_path/../Frameworks");
            }

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                [$"-o {ExecutableName}",
                ..DebugFlags,
                ..OSFlags,
                ..Shared.RequiredLibraryLinkFiles,
                ..Shared.ThirdPartyLibraries.LinkFiles,
                string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(ExecutableName);
        }

        public void RunStaticLibraryTask()
        {
            throw new NotImplementedException();
        }

        public void RunDynamicLibraryTask()
        {
            throw new NotImplementedException();
        }

        public void HandleCachedLibrary(BasaltLibraryCache CachedLibrary)
        {
            if (CachedLibrary.LibType == ELibraryType.Dynamic
                && Shared.CompilerMode == EBinaryType.Executable)
            {
                string LibFileName = LibraryFile(CachedLibrary.Name);
                File.Move(
                    Path.Join(
                       Shared.GetDebugOrReleaseDir(),
                       LibFileName),
                    Path.Join(
                        BasaltMacAppPackage.GetOrCreatePackageFolder(Shared, "Frameworks"),
                        LibFileName), true);
            }
        }
    }
}