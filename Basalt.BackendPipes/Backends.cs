using Basalt.BackendPipes.LLVM;
using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes
{
    public class BasaltCompilerBackends
    {
        public static IProvider GetCompilerBackend(BasaltProject Project, string[] Args)
        {
            int CompilerFunctions = 0;
            IProvider? ProviderBackend = null;

            foreach (IPartialNode Node in Project.Nodes)
            {
                if (Node is not LavaFunctionNode) continue;
                LavaFunctionNode NodeFunction = (LavaFunctionNode)Node;

                switch (NodeFunction.Key)
                {
                    case "UseClang":
                        CompilerFunctions++;
                        ProviderBackend = new LlvmProvider(Project, NodeFunction.Invoke<FUseCompiler>(), Args);
                        break;
                }
            }
            if (CompilerFunctions > 1)
            {
                throw new BasaltException("More than one compiler functions were used");
            } else if (CompilerFunctions == 1 && ProviderBackend != null)
            {
                return ProviderBackend;
            }

            BasaltLogger.WriteLine("No function declaring a compiler were used, defaulting to %bLLVM%c");

            // Creating a basic FUseCompiler class because there was no function in the project file
            return new LlvmProvider(Project, new("LLVM", BasaltDefaults.CPP), Args);
        }
    }
}
