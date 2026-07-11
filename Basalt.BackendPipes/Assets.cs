using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace Basalt.BackendPipes
{
    public class BasaltAssetsPipeline(
        bool TrackFiles = false)
    {
        public List<string> CopiedFiles = [];

        public void CopyFiles(string Input, string Output)
        {
            string[] Dirs = Directory.GetDirectories(Input);
            Directory.CreateDirectory(Path.Combine(Output, Input));
            EnumerateFiles(Input, Output, true);

            foreach (string DP in Dirs)
            {
                string OutputDir = Path.Combine(Output, DP);
                Directory.CreateDirectory(OutputDir);
                EnumerateFiles(DP, OutputDir, false);
            }
        }

        public void EnumerateFiles(
            string DirPath, string OutPath, bool TopLevelOnly = false)
        {
            foreach (string InputFileName in Directory.EnumerateFiles(DirPath, "*", SearchOption.TopDirectoryOnly))
            {
                string FileName = Path.GetFileName(InputFileName);
                string SecondFilepath = TopLevelOnly ? DirPath : "";

                string FilePath = Path.Combine(
                    OutPath, SecondFilepath, FileName);

                File.Copy(InputFileName, FilePath, true);
                if (TrackFiles)
                {
                    CopiedFiles.Add(Path.Combine(SecondFilepath, FileName));
                }
            }
        }
    }
}
