using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltDefaults
    {
        public static int CPP = 20;
        public static int C = 23;

        public static Dictionary<int, string> CPPVersions = new()
        {
            { 23, "c++23" },
            { 20, "c++20" },
            { 17, "c++17" },
            { 14, "c++14" },
            { 11, "c++11" },
        };

        public static Dictionary<int, string> CVersions = new()
        {
            { 23, "c23" },
            { 17, "c17" },
            { 11, "c11" },
            { 99, "c99"  },
            { 89, "c89" }
        };
    }
}
