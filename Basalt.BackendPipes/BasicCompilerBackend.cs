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

namespace Basalt.BackendPipes
{
    public enum ECompiler
    {
        MicrosoftVisualStudioCompiler,
        LLVM
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

        // FLAGS
        public bool DebugMode { get; set; } = false;

        public bool PreviewMode { get; set; } = false;

        // This is obsolete now, but i wont remove it until later
        private bool UsingDatabaseFile { get; }

        public BasicProvider(string CompilerName, string[] Args, bool UsingDatabaseFile)
        {
            BasaltLogger.WriteLine($"Selected %b{CompilerName}%c as the Backend compiler");

            this.UsingDatabaseFile = UsingDatabaseFile;

            // setting up flags
            foreach (string Flag in Args)
            {
                switch (Flag)
                {
                    case "-debug":
                        DebugMode = true;
                        BasaltGlobalFileCache.DebugMode = true;
                        break;
                    case "-staging":
                        PreviewMode = true;
                        break;
                }
            }
            BasaltGlobalFileCache.LoadIntoCache();
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

        public string GetExecutableName(string AppName)
        {
            string ArchName = GetOSArch();
            string OSName = BasicCompilerBackend.GetOSName();
            string ReleaseModel = (PreviewMode, DebugMode) switch
            {
                (true, _) => "Preview",
                (_, true) => "Developer",
                _ => "Shipping"
            };
            return $"{AppName}-{ReleaseModel}-{OSName}{ArchName}{(OperatingSystem.IsWindows() ? ".exe" : "")}";
        }

        public string GetDebugOrReleaseDir()
        {
            string DebugDir = BasaltDirectoryTiles.Debug.Value;
            string ReleaseDir = BasaltDirectoryTiles.Release.Value;

            if (DebugMode)
            {
                BasicCompilerBackend.CreateDir(DebugDir);
                return DebugDir;
            }
            else
            {
                BasicCompilerBackend.CreateDir(ReleaseDir);
                return ReleaseDir;
            }
        }

        public void SetCompilerMode(EBinaryType BinaryType, ELibraryType? LibraryType)
        {
            CompilerMode = BinaryType;
            string BinaryMessage = BinaryType == EBinaryType.Executable
                ? "Executable" : $"{LibraryType} Library";
            BasaltLogger.WriteLine(
                $"Compile mode is set to %b{BinaryMessage}%c");
        }

        public void Finish(BasaltProject Project)
        {
            LavaArrayNode? AssetArrayNode = (LavaArrayNode?)Project.GetNode("Assets");

            if (AssetArrayNode != null)
            {
                BasaltAssetsPipeline Pipeline = new(true);
                foreach (string Path in AssetArrayNode.Value)
                {
                    if (File.Exists(Path))
                    {
                        File.Copy(Path, $"{GetDebugOrReleaseDir()}/{Path}");
                        continue;
                    }
                    Pipeline.CopyFiles(Path, GetDebugOrReleaseDir());
                }
            }
            BasaltGlobalFileCache.WriteIntoCache();
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
        public static void ExecuteTool(string ToolUrl, List<string> Flags)
        {
            try
            {
                BasaltLogger.WriteLine(string.Join(" ", Flags));
                Process ToolProcess = new();
                ToolProcess.StartInfo.FileName = ToolUrl;
                ToolProcess.StartInfo.Arguments = string.Join(" ", Flags);
                ToolProcess.StartInfo.RedirectStandardOutput = true;
                ToolProcess.Start();

                while(!ToolProcess.StandardOutput.EndOfStream)
                {
                    string? Line = ToolProcess.StandardOutput.ReadLine();
                    string? LineErr = ToolProcess.StandardError.ReadLine();
                    if (!string.IsNullOrEmpty(Line)) BasaltLogger.WriteLine(Line);
                    if (!string.IsNullOrEmpty(LineErr)) BasaltLogger.WriteLine(LineErr);
                }
            } catch(BasaltException e)
            {
                throw new BasaltException($"Could not compile project, reason:\n {e}");
            }
        }

        public static void CreateDir(string Path)
        {
            if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
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

        public static List<string> GetOSFlags(List<IPartialNode> Nodes)
        {
            foreach (IPartialNode Node in Nodes.Where(NF => NF.Type == ENodeEntityType.Array))
            {
                if (OperatingSystem.IsWindows() && Node.Key == "Win32Flags") 
                    return ((LavaArrayNode)Node).Value;
                if (OperatingSystem.IsLinux() && Node.Key == "LinuxFlags") 
                    return ((LavaArrayNode)Node).Value;
                if (OperatingSystem.IsMacOS() && Node.Key == "DarwinFlags") 
                    return ((LavaArrayNode)Node).Value;
            }
            return [];
        }
    }
}
