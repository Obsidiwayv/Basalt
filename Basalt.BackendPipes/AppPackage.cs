namespace Basalt.BackendPipes;

public class BasaltMacAppPackage
{
    public static void CreateAppFolder(BasicProvider Provider)
    {
        Directory.CreateDirectory(GetFolderName(Provider));
    }

    public static string GetOrCreatePackageFolder(BasicProvider Provider, string FolderName)
    {
        string PackageFolderOutput = GetPackageName(FolderName, Provider);
        Directory.CreateDirectory(PackageFolderOutput);
        return PackageFolderOutput;
    }

    public static string GetPackageName(string Folder, BasicProvider Provider) => Path.Join(
            GetFolderName(Provider),
            Folder);

    public static string GetFolderName(BasicProvider Provider) => 
        Path.Combine(Provider.GetDebugOrReleaseDir(), $"{Provider.ProjectName.Value}.app");
}