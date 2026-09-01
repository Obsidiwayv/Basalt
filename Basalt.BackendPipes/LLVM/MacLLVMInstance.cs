using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
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
            List<string> Objects = Shared.CompileSourcesToObjects(Shared.ReleaseType);
            string MacPackageOutput = BasaltMacAppPackage
                .GetOrCreatePackageFolder(Shared, "MacOS");

            List<BasaltLibraryCache> CachedDynamicLibraries = [
                .. BasaltGlobalFileCache.LibraryCache.Where(L => L.LibType == ELibraryType.Dynamic)];

            string ExecutableName = Path.Join(
                MacPackageOutput,
                Shared.GetExecutableName());

            List<string> PackagingFlags = [];
            if (SharedLLVMInstance.ReleaseMode)
            {
                PackagingFlags.Add("-g");
            }
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
                string.Join(" ", Objects)]);

            if (SharedLLVMInstance.ReleaseMode && !Shared.Project.MacroIsPresent("DisableDSYM"))
            {
                if (Shared.VersionNumber == null)
                    throw new BasaltException("@AppVersion is null");

                string DebugSymbolsName = $"DebugSymbols-{Shared.ProjectName.Value}-{BasicProvider.GetOSArch()}-{Shared.VersionNumber.Value}";
                string DebugSymbolsPath = Path.Join(
                    BasaltDirectoryTiles.Symbols.Value,
                    $"{DebugSymbolsName}.dSYM"
                );

                BasicCompilerBackend.ExecuteTool("dsymutil", 
                [
                    ExecutableName, "-o", DebugSymbolsPath
                ]);
                BasicCompilerBackend.ExecuteTool("strip", ["-S", ExecutableName]);
                
                string ZipPath = Path.Join("Bin", $"{DebugSymbolsName}.zip");
                if (File.Exists(ZipPath)) File.Delete(ZipPath);

                ZipFile.CreateFromDirectory(
                    DebugSymbolsPath,
                    ZipPath,
                    CompressionLevel.Optimal,
                    true
                );
                File.Copy(ZipPath, Path.Join(BasaltDirectoryTiles.Release.Value, "DebugSymbols.zip"), true);
                File.Copy(ZipPath, Path.Join(BasaltDirectoryTiles.Symbols.Value, $"{DebugSymbolsName}.zip"), true);
                Directory.Delete(DebugSymbolsPath, true);
            }

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

            string DylibName = LibraryFile(Shared.ProjectName.Value);

            string BinaryName = Path.Join(
                BasicProvider.GetDebugOrReleaseDir(),
                LibraryFile(Shared.ProjectName.Value));

            BasaltGlobalFileCache.LibraryCache.Add(
               new(
                   Shared.ProjectName.Value,
                   ELibraryType.Dynamic,
                   Shared.Project.FileSource.Name, BinaryName, Includes));

            BasicCompilerBackend.ExecuteTool(SharedLLVMInstance.GetClangExecutableCommand(false),
                        [$"-o {BinaryName}",
                        "-dynamiclib",
                        $"-install_name @rpath/{DylibName}",
                        ..ExtraFlags,
                        ..Shared.GetCompilerDebugFlags(),
                        ..Shared.ThirdPartyLibraries.LinkFiles,
                        ..Shared.RequiredLibraryLinkFiles,
                        string.Join(" ", Objects)]);

            BasaltGlobalFileCache.PushFile(BinaryName);
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
                string ResourcesDir = BasaltMacAppPackage
                    .GetOrCreatePackageFolder(Shared, "Resources");

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

            // this is the parent project
            if (Shared.Project.ParentProject == null)
            {
                LavaStringNode BundleId = (LavaStringNode?)Shared.Project.GetNode("BundleId")
                    ?? throw new BasaltException("BundleId is required for executables on MacOS");
                LavaStringNode Version = Shared.VersionNumber
                    ?? throw new BasaltException("A version is needed for this bundle");
                LavaStringNode? MinOS = (LavaStringNode?)Shared.Project.GetNode("MinimumMacOSVersion");
                LavaStringNode? Icon = (LavaStringNode?)Shared.Project.GetNode("Icon");

                XmlDocument Doc = new();

                XmlDeclaration XMLDec = Doc.CreateXmlDeclaration(
                    "1.0",
                    "UTF-8",
                    null
                );
                Doc.InsertBefore(XMLDec, Doc.DocumentElement);

                XmlDocumentType DocType = Doc.CreateDocumentType(
                    "plist",
                    "-//Apple//DTD PLIST 1.0//EN",
                    "http://www.apple.com/DTDs/PropertyList-1.0.dtd",
                    null
                );
                Doc.AppendChild(DocType);

                string ResourcesDir = BasaltMacAppPackage
                    .GetOrCreatePackageFolder(Shared, "Resources");

                XmlElement PListNode = Doc.CreateElement(string.Empty, "plist", string.Empty);
                PListNode.SetAttribute("version", "1.0");

                XmlElement DictNode = Doc.CreateElement(string.Empty, "dict", string.Empty);

                Dictionary<string, string> KeysAndStrings = new()
                {
                    { "CFBundleName", Shared.ProjectName.Value },
                    { "CFBundleExecutable", Shared.GetExecutableName() },
                    { "CFBundleIdentifier", BundleId.Value },
                    { "CFBundleVersion", Version.Value },
                    { "CFBundlePackageType", "APPL" }
                };

                if (Icon != null)
                {
                    string IconFileName = Path.GetFileName(Icon.Value);
                    File.Copy(
                        Icon.Value,
                        Path.Join(ResourcesDir, IconFileName),
                        true
                    );
                    KeysAndStrings["CFBundleIconFile"] = IconFileName;
                }
                if (MinOS != null)
                {
                    KeysAndStrings["LSMinimumSystemVersion"] = MinOS.Value;
                }

                foreach (KeyValuePair<string, string> BundleInformation in KeysAndStrings)
                {
                    PutXMLValue(Doc, DictNode, BundleInformation.Key, BundleInformation.Value);
                }


                XmlElement ElementKey = Doc.CreateElement(
                    string.Empty,
                    "key",
                    string.Empty
                );
                XmlText TextKey = Doc.CreateTextNode("NSHighResolutionCapable");

                ElementKey.AppendChild(TextKey);
                DictNode.AppendChild(ElementKey);

                XmlElement TrueNode = Doc.CreateElement("true");
                DictNode.AppendChild(TrueNode);

                // Put everything into the dictionary
                PListNode.AppendChild(DictNode);
                Doc.AppendChild(PListNode);

                Doc.Save(Path.Join(
                    BasaltMacAppPackage.GetAppFolderFromName(Shared, Shared.ProjectName.Value),
                    "Info.plist"
                ));
            }

        }

        private static void PutXMLValue(
            XmlDocument Document, XmlElement DictElement, string Key, string Value)
        {
            XmlElement ElementKey = Document.CreateElement(
                string.Empty,
                "key",
                string.Empty
            );
            XmlText TextKey = Document.CreateTextNode(Key);

            ElementKey.AppendChild(TextKey);
            DictElement.AppendChild(ElementKey);

            XmlElement ElementString = Document.CreateElement(
                string.Empty,
                "string",
                string.Empty
            );
            XmlText TextString = Document.CreateTextNode(Value);

            ElementString.AppendChild(TextString);
            DictElement.AppendChild(ElementString);
        }
    }
}