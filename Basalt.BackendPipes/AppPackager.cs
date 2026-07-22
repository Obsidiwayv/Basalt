namespace Basalt.BackendPipes;

public class BasaltMacAppPackager
{
    public static void CreateAppFolder(BasicProvider Provider)
    {
        Directory.CreateDirectory(Path.Combine(
            Provider.GetDebugOrReleaseDir(), $"{Provider.ProjectName}.app"));
    }
}