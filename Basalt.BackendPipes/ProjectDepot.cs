using System.Text.Json;

namespace Basalt.BackendPipes;

public class BasaltProjectDepot(BasicProvider Provider)
{
    public void WriteMetadata()
    {
        object MetdataJSON = new
        {
            ID = Provider.BuildId,
            DepotName = Provider.ProjectName.Value
        };
        File.WriteAllText(
            Path.Join(Provider.GetProjectDepotDir(), "project.json"),
            JsonSerializer.Serialize(MetdataJSON, BasicCompilerBackend.));
    }
}