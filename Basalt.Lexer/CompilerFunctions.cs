using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public enum CompilerLanguage
    {
        CPP,
        C
    }

    public class CompilerFunctions
    {
        public static int VerifyLanguageVerson(List<string> Params, LavaFunctionNode Node, CompilerLanguage Lang)
        {
            bool IsCPP = Lang == CompilerLanguage.CPP;
            int LanguageVersion = IsCPP ? BasaltDefaults.CPP : BasaltDefaults.C;
            if (Params.Count == 1)
            {
                if (int.TryParse(Params[0], out int LangVersion))
                {
                    LanguageVersion = LangVersion;
                }
                else
                {
                    throw new BasaltException($"Parameter 1 of %m{Node.Key}()%c is not an integer!");
                }
            }
            Dictionary<int, string> LanguageDict = IsCPP 
                ? BasaltDefaults.CPPVersions 
                : BasaltDefaults.CVersions;

            if (!LanguageDict.ContainsKey(LanguageVersion))
            {
                throw new BasaltException($"%m{LanguageVersion}%c is not a valid language version!");
            }
            return LanguageVersion;
        }
    }
}
