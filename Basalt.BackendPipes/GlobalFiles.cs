using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Basalt.BackendPipes
{
    public class BasaltGlobalFileCache
    {
        public static List<CompiledFileWithHash> Compiled { get; } = [];
        public static List<CompiledFileWithHash> CachedCompiledFiles { get; } = [];
        public static List<BasaltLibraryCache> LibraryCache { get; } = [];

        public static bool DebugMode { get; set; } = false;

        public static string CacheFile
        {
            get
            {
                string HashFile = ".hashes";
                if (BasicProvider.DebugMode) HashFile = ".hashes_debug";
                if (BasicProvider.PreviewMode) HashFile = ".hashes_preview";
                return Path.Combine("Bin", HashFile);
            }
        }

        /**
         * Load the file name and hash data into cache
         */
        public static void LoadIntoCache()
        {
            if (File.Exists(CacheFile))
            {
                foreach (string Line in File.ReadAllLines(CacheFile))
                {
                    string[] SplitLine = Line.Split(",");
                    CachedCompiledFiles.Add(new(SplitLine[0], SplitLine[1]));
                }
            }
        }

        public static bool HasFile(string Path)
        {
            foreach (CompiledFileWithHash Cache in CachedCompiledFiles)
            {
                if (Path == Cache.FileName) return true;
            }
            return false;
        }

        public static void WriteIntoCache()
        {
            List<string> Lines = [];
            foreach (CompiledFileWithHash CacheLine in Compiled)
            {
                Lines.Add(CacheLine.ToString());
            }
            try
            {
                File.WriteAllText(CacheFile, string.Join("\n", Lines));
            } catch
            {
                throw new BasaltException($"Could not write {CacheFile}");
            }
        }

        public static void PushFile(string FileName)
        {
            Compiled.Add(new(FileName, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FileName)))));
        }
    }

    public class CompiledFileWithHash(string FileName, string Hash)
    {
        public string FileName { get; } = FileName.Replace('\\', '/');
        public string Hash { get; } = Hash;

        public override string ToString()
        {
            return $"{FileName},{Hash}";
        }
    }
}
