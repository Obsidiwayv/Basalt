using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Basalt.BackendPipes.MSVC
{
    public class MSVCUtil
    {
        public static string BaseVSDir { get; } = @"C:/Program Files/Microsoft Visual Studio";

        public static string GetVersionedDirectory(string? ForcedVersion = null)
        {
            ;
            string[] VersionedDirectories = Directory.GetDirectories(BaseVSDir, "*", SearchOption.TopDirectoryOnly);
            if (VersionedDirectories.Length == 0)
                throw new BasaltException("There is no MSVC installation present on this machine");

            // If there is no forced version after this then it will stay as the first option
            string MSVCFolder = VersionedDirectories[0];
            if (ForcedVersion != null)
            {
                foreach (string Dir in VersionedDirectories)
                {
                    if (Path.GetDirectoryName(Dir) == ForcedVersion)
                    {
                        MSVCFolder = Dir;
                        break;
                    }
                }
                throw new BasaltException($"MSVC Version {ForcedVersion} not installed");
            }
            string? VersionName = Path.GetDirectoryName(MSVCFolder);
            return VersionName == null 
                ? throw new BasaltException("Version Folder is a root directory") 
                : GetEdition(MSVCFolder, VersionName);
        }

        // Its private due to GetVersionedDirectory using this method instead
        private static string GetEdition(string Dir, string VersionName)
        {
            string[] EditionDirs = Directory.GetDirectories(Dir, "*", SearchOption.TopDirectoryOnly);
            return EditionDirs[0];
        }
    }
}
