using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Basalt.BackendPipes.Compression
{
    public class BasaltZipHelper
    {
        public static BasaltZipFile Create(string Name, string[] Entries)
        {
            if (File.Exists(Name))
            {
                File.Delete(Name);
            }

            using ZipArchive ArchiveFile = ZipFile.Open(Name, ZipArchiveMode.Create);
            foreach (string E in Entries)
            {
                ArchiveFile.CreateEntryFromFile(E, Path.GetFileName(E));
            }
            return new(Name);
        }
    }
}
