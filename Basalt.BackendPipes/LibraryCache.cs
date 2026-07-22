using System;
using System.Collections.Generic;
using System.Text;
using Basalt.LavaLang.Functions;

namespace Basalt.BackendPipes
{

    public class BasaltLibraryCache(string LibraryName, ELibraryType Type, string ProjectFilePath, string OutputFile, List<string> Includes)
    {
        public string Name { get; } = LibraryName;
        public ELibraryType LibType { get; } = Type;
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
