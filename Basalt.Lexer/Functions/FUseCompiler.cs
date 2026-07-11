using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Functions
{

    /**
     * This class can applied to UseMSVC(), and UseClang()
     */
    public class FUseCompiler(string _CompilerName, int _LanguageVersion)
    {
        public string CompilerName { get; } = _CompilerName;

        public int LanguageVersion { get; } = _LanguageVersion;
    }
}
