using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Basalt.Tile
{
    /**
     * A basic compilation database class for clang
     */
    public class ClangCompilationDatabase
    {
        [JsonPropertyName("directory")]
        public required string Directory { get; set; }

        [JsonPropertyName("arguments")]
        public required List<string> Arguments { get; set; }

        [JsonPropertyName("file")]
        public required string File { get; set; }
    }

    public class BasaltCompilationDatabase
    {
        public static List<ClangCompilationDatabase> DatabaseList = [];

        private static readonly JsonSerializerOptions JSONFormattingOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static void PushEntry(string Dir, List<string> Args, string File)
        {
            ClangCompilationDatabase DatabaseEntry = new()
            {
                Directory = Dir,
                Arguments = Args,
                File = File
            };
            DatabaseList.Add(DatabaseEntry);
        }

        public static void WriteFile()
        {
            File.WriteAllText("compile_commands.json", 
                JsonSerializer.Serialize(DatabaseList, JSONFormattingOptions));
        }
    }
}
