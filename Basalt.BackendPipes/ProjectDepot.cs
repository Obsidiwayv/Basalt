using System.Text.Json;
using System.Text.Json.Serialization;
using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;

namespace Basalt.BackendPipes;

public class BasaltProjectDepot(BasicProvider Provider)
{
    public static string BinaryExecutableString = "EXECUTABLE";
    public static string LibraryStaticString = "STATIC_LIBRARY";
    public static string LibrarySharedString = "SHARED_LIBRARY";

    public void WriteMetadata()
    {
        LavaArrayNode? Libraries = (LavaArrayNode?)Provider.Project.GetNode("Using");

        BasaltDepotMetdata Metdata = new()
        {
            ID = Provider.BuildId,
            Type = GetBinaryType(),
            Name = Provider.ProjectName.Value,
            ProjectFilePath = Provider.Project.FileSource.Name
        };

        if (Libraries != null && Libraries.Value.Count != 0)
        {
            foreach (string DepotFile in Directory.EnumerateFiles(
                BasaltDirectoryTiles.Depots.Value, "*.json", SearchOption.AllDirectories
            ))
            {
                BasaltDepotMetdata? Module = JsonSerializer.Deserialize<BasaltDepotMetdata>(
                    File.ReadAllText(DepotFile));
                if (Module == null) 
                    continue;
                if (Libraries.Value.Contains(Module.ProjectFilePath))
                {
                    Metdata.Modules.Add(new()
                    {
                        ID = Module.ID,
                        Name = Module.Name,
                        LinkFile = Module.LinkFile,
                        RuntimeFile = Module.RuntimeFile
                    });
                }
            }
        }

        File.WriteAllText(
            Path.Join(Provider.GetProjectDepotDir(), "project.json"),
            JsonSerializer.Serialize(Metdata, BasicCompilerBackend.JsonOutputOptions));
    }

    private BasaltDepotBinaryType GetBinaryType()
    {
        if (Provider.CompilerMode == EBinaryType.Executable)
        {
            return BasaltDepotBinaryType.Executable;
        }
        if (Provider.CompilerMode == EBinaryType.Library)
        {
            if (Provider.LibraryType == ELibraryType.Static)
            {
                return BasaltDepotBinaryType.LibraryStatic;
            } else
            {
                return BasaltDepotBinaryType.LibraryShared;
            }
        }
        return BasaltDepotBinaryType.None;
    }

    public class BasaltDepotMetdata
    {
        [JsonPropertyName("identifier")]
        public Guid ID { get; set; }

        [JsonPropertyName("project_file")]
        public string ProjectFilePath { get; set; }

        [JsonPropertyName("depot")]
        public string Name { get; set; }

        [JsonPropertyName("binary_type")]
        public BasaltDepotBinaryType Type { get; set; }

        [JsonPropertyName("runtime_file")]
        public string? RuntimeFile { get; set; }

        [JsonPropertyName("link_file")]
        public string? LinkFile { get; set; }

        [JsonPropertyName("modules")]
        public List<BasaltDepotModuleMetdata> Modules { get; set; } = [];
    }

    public class BasaltDepotModuleMetdata
    {
        [JsonPropertyName("identifier")]
        public Guid ID { get; set; }

        [JsonPropertyName("depot")]
        public string Name { get; set; }

        [JsonPropertyName("runtime_file")]
        public string? RuntimeFile { get; set; }

        [JsonPropertyName("link_file")]
        public string? LinkFile { get; set; }
    }

    public enum BasaltDepotBinaryType
    {
        Executable,
        LibraryStatic,
        LibraryShared,
        None
    }
}