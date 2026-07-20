using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltGlobalStats
    {
        public static int ExecutedFunctions { get; set; } = 0;

        /**
         * How deep the the build tool is working on with a project
         * <example>ProjectB - Depth 1, => ProjectA = Depth 2</example>
         */
        public static int Depth { get; set; } = 0;
    }
}
