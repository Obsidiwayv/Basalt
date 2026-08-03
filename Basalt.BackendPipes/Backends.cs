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
        /**
         * Method that invokes different types of compilers
         * the load order of functions also matter:
         * UseMSVC(version) -> first to be invoked
         * UseClang(version) -> Ignored
         * 
         * Swapping the positions of these functions will change the behaviour of the build pipeline
         * some providers may not have support for some operating systems
         */
        public static IProvider GetCompilerBackend(BasaltProject Project, string[] Args)
        {
            int CompilerFunctions = 0;
            IProvider? ProviderBackend = null;

            foreach (IPartialNode Node in Project.Nodes)
            {
                if (Node is not LavaFunctionNode) continue;
                if (CompilerFunctions == 1) break; // Stop continuing after invoking a function

                LavaFunctionNode NodeFunction = (LavaFunctionNode)Node;

                switch (NodeFunction.Key)
                {
                    case "UseClang":
                        CompilerFunctions++;
                        ProviderBackend = new LLVMProvider(
                            Project, NodeFunction.Invoke<FUseCompiler>(), Args);
                        break;
                }
            }
            //if (CompilerFunctions > 1)
            //{
            //    throw new BasaltException("More than one compiler functions were used");
            //}
            if (ProviderBackend != null)
            {
                return ProviderBackend;
            }

            BasaltLogger.WriteLine("No function declaring a compiler were used, defaulting to %bLLVM%c");

            // Creating a basic FUseCompiler class because there was no function in the project file
            return new LLVMProvider(Project, new("LLVM", BasaltDefaults.CPP), Args);
        }
    }
}
