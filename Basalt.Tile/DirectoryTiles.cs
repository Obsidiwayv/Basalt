using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.Tile
{
    public class BasaltDirectoryTiles
    {
        private readonly static List<PathTile> Paths =
        [
            new("binary", @"Bin"),
            new("program_bin_dev", @"Bin/Debug", CheckPriority.None),
            new("program_bin_shipping", @"Bin/Release", CheckPriority.None),
            new("object", @"Bin/Object"),
            new("libs", @"Bin/Libraries")
        ];


        // Getting path tiles from the dictionary and storing them into properties
        public static PathTile Debug { get; } = Paths[1];
        public static PathTile Release { get; } = Paths[2];
        public static PathTile Object { get; } = Paths[3];
        public static PathTile Libraries { get; } = Paths[4];

        public static void Verify()
        {
            foreach (PathTile Path in Paths)
            {
                if (Path.Priority == CheckPriority.None) continue;
                if (!Directory.Exists(Path.ToString()))
                {
                    Directory.CreateDirectory(Path.ToString());
                    BasaltLogger.WriteLine($"Created directory {Path}");
                }
            }

        }

        public static PathTile? Get(string KeyName)
        {
            return Paths.Find(Tile => Tile.Key == KeyName);
        }
    }

    /**
     * Checking Priority of the directory in the Verify function
     */
    public enum CheckPriority
    {
        None,
        All
    }

    public sealed class PathTile(string Key, string Value, CheckPriority Priority = CheckPriority.All) 
    {
        public string Key { get; } = Key;
        public string Value { get; } = Value;
        public CheckPriority Priority { get; } = Priority;

        public override string ToString()
        {
            return Value;
        }
    }
}
