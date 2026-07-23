using Basalt.BackendPipes.LLVM;
using Basalt.LavaLang;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;

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

            List<BasaltLibraryCache> CachedDynamicLibraries = [
                .. BasaltGlobalFileCache
                    .LibraryCache.Where(L => L.LibType == ELibraryType.Dynamic)];

            string ExecutableName = Path.Join(
                MacPackageOutput,
                Shared.GetExecutableName());

            List<string> PackagingFlags = [];
            if (CachedDynamicLibraries.Count != 0)
            {
                string OutputFrameworkPath = BasaltMacAppPackage
                    .GetOrCreatePackageFolder(Shared, "Frameworks");
                PackagingFlags.Add("-Wl,-rpath,@executable_path/../Frameworks");
            }

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                [$"-o {ExecutableName}",
                ..DebugFlags,
                ..PackagingFlags,
                ..Shared.RequiredLibraryLinkFiles,
                ..Shared.ThirdPartyLibraries.LinkFiles,
                ..Shared.ThirdPartyLibraries.Names.Select(Lib => $"-l{Lib}"),
                string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(ExecutableName);
        }

        public void RunStaticLibraryTask()
        {
            List<string> Objects = Shared.CompileSourcesToObjects();
            List<string> Includes = Shared.GetIncludes();

            string Arch = BasicProvider.GetOSArch();
            string LibraryName = $"{Shared.ProjectName.Value}-x{Arch}";

            string LibraryOutputPath = $"{Path.Join(BasaltDirectoryTiles.Libraries.Value, LibraryName)}-static.a";

            BasicCompilerBackend.ExecuteTool("libtool", [
                "-static",
                "-o",
                LibraryOutputPath,
                ..Objects]);

            BasaltGlobalFileCache.LibraryCache.Add(
                new(Shared.ProjectName.Value,
                    ELibraryType.Static,
                    Shared.Project.FileSource.Name,
                    LibraryOutputPath,
                    Includes));
        }

        public void RunDynamicLibraryTask()
        {
            List<string> Objects = Shared.CompileSourcesToObjects();
            List<string> Includes = Shared.GetIncludes();
            List<string> ExtraFlags = [];

            string BinaryName = Path.Join(
                Shared.GetDebugOrReleaseDir(),
                LibraryFile(Shared.ProjectName.Value));

            BasaltGlobalFileCache.LibraryCache.Add(
               new(
                   Shared.ProjectName.Value,
                   ELibraryType.Dynamic,
                   Shared.Project.FileSource.Name, BinaryName, Includes));

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                        [$"-o {BinaryName}",
                        "-dynamiclib",
                        ..ExtraFlags,
                        ..Shared.GetCompilerDebugFlags(),
                        ..Shared.ThirdPartyLibraries.LinkFiles,
                        string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(BinaryName);
        }

        // This method is unused on MacOS as dylibs 
        public void HandleCachedLibrary(BasaltLibraryCache CachedLibrary)
        {
            return;
        }
    }
}