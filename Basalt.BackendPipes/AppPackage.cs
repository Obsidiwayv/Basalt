namespace Basalt.BackendPipes;

public class BasaltMacAppPackage
{
    public static void CreateAppFolder(BasicProvider Provider)
    {
        Directory.CreateDirectory(GetFolderName(Provider));
    }

    public static string GetOrCreatePackageFolder(BasicProvider Provider, string FolderName)
    {
        string PackageFolderOutput = Path.Join(
            GetFolderName(Provider),
            FolderName
        );
        Directory.CreateDirectory(PackageFolderOutput);
        return PackageFolderOutput;
    }

    public static string GetFolderName(BasicProvider Provider) => 
        Path.Combine(Provider.GetDebugOrReleaseDir(), $"{Provider.ProjectName.Value}.app");
}