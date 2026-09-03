using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Basalt.BackendPipes
{
    public enum ECompiler
    {
        MicrosoftVisualStudioCompiler,
        LLVM,
        XCode
    }

    public enum EReleaseMode
    {
        Shipping = 0x01,
        Preview = 0x02,
        Debug = 0x03
    }

    public enum OSInformation
    {
        Windows,
        MacOS,
        Linux,
        FreeBSDBased
    }

    public class BasicProvider
    {
        // If the Compiler mode is null, its not set yet
        public EBinaryType? CompilerMode { get; set; }

        public ELibraryType? LibraryType { get; set; }

        // FLAGS
        public static bool DebugMode { get; set; } = false;

        public static bool PreviewMode { get; set; } = false;

        public bool DepthLogging { get; set; } = false;

        public bool PackageIntoZip { get; set; } = false;

        public static bool ReleaseMode { get; set; } = true;

        public static bool VerboseMode { get; set; } = false;

        public bool SkipBuild { get; } = false;

        public EReleaseMode ReleaseType { get; } = EReleaseMode.Shipping;

        public bool HasFlags { get; set; } = false;

        // This is obsolete now, but i wont remove it until later
        private bool UsingDatabaseFile { get; }

        public BasaltProject Project { get; }

        public LavaStringNode ProjectName { get; }

        public LavaStringNode? VersionNumber { get; }

        public string OutputDirectory { get; set; }

        public string DepotOutDirectory { get; set; }

        public Guid BuildId { get; }

        public BasaltProjectDepot Depot { get; }

        public Dictionary<string, string> SourceHashes { get; set; } = [];

        public BasicProvider(
            BasaltProject Project,
            string CompilerName,
            string[] Args,
            bool UsingDatabaseFile)
        {
            BasaltLogger.WriteLine($"Selected %b{CompilerName}%c as the Backend compiler");
            BasaltGlobalStats.Depth++;

            this.Project = Project;

            ProjectName = (LavaStringNode?)Project.GetNode("name")
                ?? throw new BasaltException("Project is missing a name attribute!");

            BasaltGlobalFileCache.ProjectNames.Add(ProjectName.Value);

            VersionNumber = (LavaStringNode?)Project.GetNode("AppVersion");

            this.UsingDatabaseFile = UsingDatabaseFile;

            // Ignore already set flags for performance
            if (!HasFlags)
            {
                // setting up flags
                foreach (string Flag in Args)
                {
                    switch (Flag)
                    {
                        case "-debug":
                            DebugMode = true;
                            ReleaseMode = false;
                            ReleaseType = EReleaseMode.Debug;
                            BasaltGlobalFileCache.DebugMode = true;
                            break;
                        case "-staging":
                            PreviewMode = true;
                            ReleaseMode = false;
                            ReleaseType = EReleaseMode.Preview;
                            break;
                        case "-ld":
                            DepthLogging = true;
                            break;
                        case "-zip":
                            if (DebugMode)
                            {
                                throw new BasaltException("Apps cannot be packaged in debug mode!");
                            }
                            PackageIntoZip = true;
                            break;
                        case "-verbose":
                            VerboseMode = true;
                            break;
                        default:
                            break;
                    }
                }
                HasFlags = true;
            }

            OutputDirectory = UpdateOutputDirectory();
            BasaltGlobalFileCache.LoadIntoCache();

            bool bHasChanges = VerifyHashes();
            if (!bHasChanges)
            {
                SkipBuild = true;
            } else
            {
                BuildId = Guid.CreateVersion7();
            }

            Depot = new(this);
        }

        public static string GetOSArch() => RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "64",
            Architecture.X86 => "32",
            Architecture.Arm => "Arm32",
            Architecture.Arm64 => "Arm64",
            _ => throw new BasaltException("%rUnknown OS architecture%c")
        };

        public static string GetLibFileExtension() => BasicCompilerBackend.GetOSEnum() switch
        {
            OSInformation.Linux => "so",
            OSInformation.MacOS => "dylib",
            OSInformation.Windows => "dll",
            _ => throw new BasaltException("Unknown Operating system library type")
        };

        public string GetExecutableName(bool WithExt = true)
        {
            string ArchName = GetOSArch();
            string OSName = BasicCompilerBackend.GetOSName();
            string ReleaseModel = (PreviewMode, DebugMode) switch
            {
                (true, _) => "Preview",
                (_, true) => "Developer",
                _ => "Shipping"
            };
            string Ext = OperatingSystem.IsWindows() && WithExt ? ".exe" : "";
            return $"{ProjectName.Value}-{ReleaseModel}-{OSName}{ArchName}{Ext}";
        }

        public string GetSourceHashFile()
        {
            string HashFileName = Path.Join("Bin", ".source_hashes");
            if (DebugMode)
            {
                HashFileName = Path.Join("Bin", ".d_source_hashes");
            }
            if (PreviewMode)
            {
                HashFileName = Path.Join("Bin", ".p_source_hashes");
            }

            return HashFileName;
        }

        private bool VerifyHashes()
        {
            string HashFileName = GetSourceHashFile();
            string HashFileContent = File.ReadAllText(HashFileName);
            if (HashFileContent == null) 
                return true;

            int Changes = 1;

            return Changes == 0;
        }

        private string UpdateOutputDirectory()
        {
            LavaStringNode? UserOutputDir = (LavaStringNode?)Project.GetNode("Output");
            if (DebugMode)
            {
                DepotOutDirectory = Path.Join(GetProjectDepotDir(), "Debug");
            }
            if (ReleaseMode)
            {
                DepotOutDirectory = Path.Join(GetProjectDepotDir(), "Release");
            }
            if (PreviewMode)
            {
                DepotOutDirectory = Path.Join(GetProjectDepotDir(), "Preview");
            }
            Directory.CreateDirectory(DepotOutDirectory);

            if (UserOutputDir != null)
            {
                if (OperatingSystem.IsMacOS())
                {
                    BasaltLogger.WriteLine("The Output attribute is ignored on MacOS and wont be used");
                }
                return Path.Join(GetDebugOrReleaseDir(), UserOutputDir.Value);
            }
            else
            {
                return GetDebugOrReleaseDir();
            }
        }

        public string GetProjectDepotDir()
        {
            string Depot = Path.Join(BasaltDirectoryTiles.Depots.Value, ProjectName.Value);
            Directory.CreateDirectory(Depot);
            return Depot;
        }

        public static string GetDebugOrReleaseDir()
        {
            string DebugDir = BasaltDirectoryTiles.Debug.Value;
            string ReleaseDir = BasaltDirectoryTiles.Release.Value;
            string PreviewDir = BasaltDirectoryTiles.Preview.Value;

            if (PreviewMode && DebugMode)
                throw new BasaltException("Preview and Debug modes cannot be active at the same time!");

            if (DebugMode)
            {
                Directory.CreateDirectory(DebugDir);
                return DebugDir;
            }
            else if (PreviewMode)
            {
                Directory.CreateDirectory(PreviewDir);
                return PreviewDir;
            }
            else
            {
                Directory.CreateDirectory(ReleaseDir);
                return ReleaseDir;
            }
        }

        public void SetCompilerMode(EBinaryType BinaryType, ELibraryType? LibraryType)
        {
            CompilerMode = BinaryType;
            this.LibraryType = LibraryType;
            string BinaryMessage = BinaryType == EBinaryType.Executable
                ? "Executable" : $"{LibraryType} Library";
            BasaltLogger.WriteLine(
                $"Compile mode is set to %b{BinaryMessage}%c");
        }

        // Old code, this is no longer used - kept for reference
        public void Finish(BasaltProject Project, string ProjectName)
        {
            LavaArrayNode? AssetArrayNode = (LavaArrayNode?)Project.GetNode("Assets");

            if (AssetArrayNode != null)
            {
                BasaltAssetsPipeline Pipeline = new();
                foreach (string Path in AssetArrayNode.Value)
                {
                    if (File.Exists(Path))
                    {
                        File.Copy(Path, $"{GetDebugOrReleaseDir()}/{Path}");
                        continue;
                    }
                    BasaltAssetsPipeline.CopyFiles(Path, GetDebugOrReleaseDir());
                }
            }
            if (DepthLogging) BasaltLogger.WriteLine($"Finished Layer {BasaltGlobalStats.Depth}");

            BasaltGlobalFileCache.WriteIntoCache();
        }

        public string ParseStringVariables(string Input)
        {
            return Input
                .Replace("#projroot", Path.GetFullPath(Project.FileSource.Name).TrimEnd(Path.DirectorySeparatorChar));
        }

        public List<string> GetOSFlags(BasaltProject Project, string FlagNodeName)
        {
            LavaArrayNode? FlagsNode = (LavaArrayNode?)Project.GetNode(FlagNodeName);
            if (FlagsNode != null)
            {
                List<string> Flags = [];

                foreach (string F in FlagsNode.Value)
                {
                    // Its a global flag
                    if (!F.Contains("://") && !F.StartsWith('@'))
                    {
                        Flags.Add(F);
                        continue;
                    }

                    string[] FlagString = F.Split("://");

                    int FlagOffset = 1;

                    EReleaseMode FlagBuildType = EReleaseMode.Shipping;

                    switch (FlagString[1])
                    {
                        case "debug":
                            FlagBuildType = EReleaseMode.Debug;
                            FlagOffset++;
                            break;
                        case "preview":
                            FlagBuildType = EReleaseMode.Preview;
                            FlagOffset++;
                            break;
                    }

                    if (FlagBuildType == EReleaseMode.Shipping && !ReleaseMode) continue;
                    if (FlagBuildType == EReleaseMode.Debug && !DebugMode) continue;
                    if (FlagBuildType == EReleaseMode.Preview && !PreviewMode) continue;

                    if (OperatingSystem.IsWindows() && FlagString[0] == "@windows")
                        Flags.Add(FlagString[FlagOffset]);
                    else if (OperatingSystem.IsLinux() && FlagString[0] == "@linux")
                        Flags.Add(FlagString[FlagOffset]);
                    else if (OperatingSystem.IsMacOS() && FlagString[0] == "@macos")
                        Flags.Add(FlagString[FlagOffset]);
                    else
                        Flags.Add(FlagString[FlagOffset]);
                }
                return Flags;
            }
            return [];
        }

        /**
         * Not used until a fix is found
         */
        private void ResetDirectory()
        {
            foreach (string FilePath in
                Directory.EnumerateFiles(GetDebugOrReleaseDir(), "*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(FilePath);
            }
            foreach (string Dir in
                Directory.EnumerateDirectories(GetDebugOrReleaseDir(), "*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(Dir, true);
            }
        }
    }

    public class BasicCompilerBackend
    {
        public static readonly JsonSerializerOptions JsonOutputOptions = new() 
        { 
            WriteIndented = true
        };

        public static string GetLibraryFile(string Name) =>
            $"lib{Name}-x{BasicProvider.GetOSArch()}.dylib";

        public static void ExecuteTool(string ToolUrl, List<string> Flags)
        {
            try
            {
                if (BasicProvider.VerboseMode)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        command = ToolUrl,
                        arguments = string.Join(" ", Flags)
                    }, JsonOutputOptions));
                } 
                Process ToolProcess = new();
                ToolProcess.StartInfo.FileName = ToolUrl;
                ToolProcess.StartInfo.Arguments = string.Join(" ", Flags);
                ToolProcess.StartInfo.RedirectStandardOutput = true;
                ToolProcess.Start();

                while (!ToolProcess.StandardOutput.EndOfStream)
                {
                    string? Line = ToolProcess.StandardOutput.ReadLine();
                    string? LineErr = ToolProcess.StandardError.ReadLine();
                    if (!string.IsNullOrEmpty(Line)) BasaltLogger.WriteLine(Line);
                    if (!string.IsNullOrEmpty(LineErr)) BasaltLogger.WriteLine(LineErr);
                }
            }
            catch (BasaltException e)
            {
                throw new BasaltException($"Could not compile project, reason:\n {e}");
            }
        }

        public static OSInformation GetOSEnum()
        {
            if (OperatingSystem.IsWindows()) return OSInformation.Windows;
            if (OperatingSystem.IsMacOS()) return OSInformation.MacOS;
            if (OperatingSystem.IsLinux()) return OSInformation.Linux;
            if (OperatingSystem.IsFreeBSD()) return OSInformation.FreeBSDBased;
            throw new BasaltException("%rUnknown Operating system%c");
        }

        public static string GetOSName()
        {
            return GetOSEnum() switch
            {
                OSInformation.Windows => "Win",
                OSInformation.Linux => "Linux",
                OSInformation.MacOS => "Mac",
                OSInformation.FreeBSDBased => "BSD",
                _ => throw new BasaltException("%rUnknown Operating system%c")
            };
        }
    }
    public class BasaltBuildId
    {
        private static int SHORT_ID_LENGTH { get; } = 6;
        // the longer version of the id, 6 x 2 which will be 12
        private static int LONG_ID_LENGTH { get; } = SHORT_ID_LENGTH * 2;

        public static string Generate(bool UseLong)
        {
            // Generate a random seed based on the GUID v7 format
            Random Rand = new(Guid.CreateVersion7().GetHashCode());
            Random OneOrTwo = new();

            int ID_LENGTH = UseLong ? LONG_ID_LENGTH : SHORT_ID_LENGTH;
            string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

            StringBuilder ID = new();
            for (int I = 0; I < ID_LENGTH; I++)
            {
                char UpperOrLower = Characters[Rand.Next(Characters.Length)];
                if (OneOrTwo.Next(0, 2) == 1 && char.IsLetter(UpperOrLower))
                {
                    UpperOrLower = char.ToLower(UpperOrLower);
                }
                ID.Append(UpperOrLower);
            }

            return ID.ToString();
        }
    }
}
