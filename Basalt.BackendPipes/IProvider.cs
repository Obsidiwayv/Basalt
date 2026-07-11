using Basalt.LavaLang;
using Basalt.LavaLang.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes
{
    public interface IProvider
    {
        FUseCompiler CompilerMetadata { get; }

        /**
         * Task building an x32, or x64 based binary executable
         */
        void BuildExecutableTask();

        /**
         * Task building a library binary
         */
        void BuildLibraryTask(ELibraryType LibraryType);

        /**
         * Method to be called when compiling is finished
         * > Copies files to the output directory
         */
        abstract void Finish(BasaltProject Project);
    }
}
