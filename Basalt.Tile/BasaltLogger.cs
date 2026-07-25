using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.Tile
{
    public class BasaltLogger
    {
        private static Dictionary<string, string> ColorCodes = new()
        {
            {"%r", "\u001b[91m"},
            {"%g", "\u001b[92m"},
            {"%b", "\u001b[96m"},
            {"%m", "\u001b[95m"},
            {"%c", "\u001b[0m"},
        };

        public static void WriteLine(string contents)
        {
            Console.WriteLine($"{DateTime.Now:T}>> {ParseColorOutput(contents)}");
        }

        public static string ParseColorOutput(string contents)
        {
            foreach (var (key, color) in ColorCodes)
            {
                contents = contents.Replace(key, color);
            }
            return contents;
        }
    }
}
