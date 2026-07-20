using Basalt.LavaLang.Functions;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    public class LavaFunctionNode(string KeyName, List<string> ValueWithType)
        : LavaArrayNode(KeyName, ValueWithType, ENodeEntityType.Function) 
    {
        public int ParamCount
        {
            get
            {
                return Value.Count;
            }
        }

        /**
         * Attempts to run the defined lava function
         * 
         * Will return a class for the specific function or null if it doesnt exist
         */
        public T Invoke<T>()
        {
            MethodInfo? AvalibleMethod = typeof(Functions)
                .GetMethod(Key, BindingFlags.Static | BindingFlags.Public) 
                ?? throw new BasaltException($"Unexpected function: {Key}()");

            BasaltGlobalStats.ExecutedFunctions++;
            return (T)AvalibleMethod.Invoke(null, [this, Value])!;
        }
    }

    internal class Functions
    {
        public static FCreateBinary CreateBinary(LavaFunctionNode Node, List<string> Params)
        {
            if (Node.ParamCount == 0)
                throw new BasaltException("CreateBinary() does not define any parameters!");

            EBinaryType BinaryType = LavaFunctionParams.GetBinaryType(Params[0]);
            ELibraryType LibraryType = ELibraryType.None;

            if (Node.ParamCount > 1)
            {
                LibraryType = LavaFunctionParams.GetLibraryType(Params[1]);
            }

            return new(BinaryType, LibraryType);
        }

        public static FUseCompiler UseClang(LavaFunctionNode Node, List<string> Params)
        {
            int LanguageVersion = CompilerFunctions.VerifyLanguageVerson(Params, Node);
            return new("LLVM", LanguageVersion);
        }

        public static bool UseC(LavaFunctionNode _, List<string> __)
        {
            BasaltLogger.WriteLine("Compiler language set to C");
            return true; // idk
        }
    }
}
