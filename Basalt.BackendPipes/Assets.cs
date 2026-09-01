namespace Basalt.BackendPipes
{
    public class BasaltAssetsPipeline(
        bool TrackFiles = false)
    {
        public List<string> CopiedFiles = [];

        public static void CopyFiles(string Input, string Output)
        {
            string Filter = "*";
            if (Input.Contains(','))
            {
                string[] SplitDirPath = Input.Split(",");
                if (SplitDirPath.Length == 1)
                    throw new BasaltException($"Expecting file filter at: %r{SplitDirPath[0]},%c");

                Filter = SplitDirPath[1];
                Input = SplitDirPath[0];
            }
            CopyDirectory(Input, Output, Filter);
        }

        private static void CopyDirectory(string Source, string Target, string Filter)
        {
            string BaseDir = Path.GetFileName(Path.GetFullPath(Source)).TrimEnd(Path.DirectorySeparatorChar);
            string TargetBaseDir = Path.Join(Target, BaseDir);

            Directory.CreateDirectory(TargetBaseDir);

            foreach (string FilePath in Directory.GetFiles(Source, Filter))
            {
                string Name = Path.GetFileName(FilePath);
                string Destination = Path.Join(TargetBaseDir, Name);
                File.Copy(FilePath, Destination, true);
            }

            foreach (string SubDirectory in Directory.GetDirectories(Source))
            {
                string Name = Path.GetFileName(SubDirectory);
                string Destination = Path.Join(TargetBaseDir, Name);
                CopyDirectory(SubDirectory, Destination, Filter);
            }
        }

        [Obsolete("Unused")]
        public void Enumerate(
            string DirPath, string OutPath, string Filter)
        {
            foreach (string InputFileName in Directory.EnumerateFiles(DirPath, Filter, SearchOption.AllDirectories))
            {
                Console.WriteLine(OutPath);
                File.Copy(InputFileName, InputFileName.Replace(DirPath, OutPath), true);
            }
        }
    }
}
