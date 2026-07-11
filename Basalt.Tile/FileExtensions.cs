using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.Tile
{
    public class BasaltFileExtensions
    {
        public static string ObjectFile = OperatingSystem.IsWindows() ? ".obj" : ".o";
    }
}
