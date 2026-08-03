using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes.Compression
{
    public class BasaltZipFile(string ZipPath)
    {
        public string FilePath { get; } = ZipPath;

        public BasaltZipFile Copy(string OutputFile)
        {
            try
            {
                File.Copy(FilePath, OutputFile);
            } catch(Exception E)
            {
                throw new BasaltException($"Zip file: {FilePath} cannot be copied to {OutputFile}\n%rREASON:%c\n{E.Message}");
            }
            return this;
        }
    }
}
