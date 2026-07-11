using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class CompilerFunctions
    {
        public static int VerifyLanguageVerson(List<string> Params, LavaFunctionNode Node)
        {
            int LanguageVersion = BasaltDefaults.CPP;
            if (Params.Count == 1)
            {
                if (int.TryParse(Params[0], out int LangVersion))
                {
                    LanguageVersion = LangVersion;
                }
                else
                {
                    throw new BasaltException($"Parameter 1 of %m{Node.Key}()%c is not a valid language version!");
                }
            }
            return LanguageVersion;
        }
    }
}
