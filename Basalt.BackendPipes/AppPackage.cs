namespace Basalt.BackendPipes;

public class BasaltMacAppPackage
{
    [Obsolete("no longer used in newer classes")]
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
            GetAppFolderFromName(Provider, Provider.ProjectName.Value),
            Folder);

    public static string GetOrCreateNamedPackageFolder(
        BasicProvider Provider, string InputFolder, string OutputFolder)
    {
        string Output = Path.Join(
            Provider.GetDebugOrReleaseDir(),
            InputFolder,
            "Contents",
            OutputFolder
        );
        Directory.CreateDirectory(Output);
        return Output;
    }

    [Obsolete("Use GetAppFolderFromName")]
    public static string GetFolderName(BasicProvider Provider) => 
        Path.Combine(Provider.GetDebugOrReleaseDir(), $"{Provider.ProjectName.Value}.app", "Contents");

    public static string GetAppFolderFromName(BasicProvider Provider, string Name) => 
        Path.Combine(Provider.GetDebugOrReleaseDir(), $"{Name}.app", "Contents");
}