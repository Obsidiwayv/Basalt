using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes
{
    public class BasaltLibraryCache(string ProjectFilePath, string OutputFile, List<string> Includes)
    {
        public string ProjectFile { get; } = ProjectFilePath;
        public string OutputFile { get; } = OutputFile;
        public List<string> Includes { get; } = Includes;
    }

    public class BasaltThirdPartyLibraries
    {
        public List<string> LinkFiles = [];

        public List<string> Headers = [];

        public List<string> Sources = [];
    }
}
