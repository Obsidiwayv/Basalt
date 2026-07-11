namespace Basalt.Build;

using Basalt.BackendPipes;
using Basalt.Build;
using Basalt.LavaLang;
using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Functions;
using Basalt.LavaLang.Impl;
using Basalt.Tile;

public class BasaltBuildProgram
{
    public static void Main(string[] Args)
    {
        BasaltLavaFile ProjectFile = GetProjectFile(Args);
        // Verify the directories before starting the compiler
        BasaltDirectoryTiles.Verify();

        BasaltProject ProjectParent = GetProject(ProjectFile);

        List<BasaltProject> ProjectGraph = BasaltTopologicalSorting.BuildDepedencyGraph(ProjectParent);

        foreach (BasaltProject Project in ProjectGraph)
        {
            LavaFunctionNode BinaryMetaFunction = (LavaFunctionNode?)Project.GetNode("CreateBinary")
                ?? throw new BasaltException("%mCreateBinary()%c wasn't defined");

            FCreateBinary BinaryMeta = BinaryMetaFunction.Invoke<FCreateBinary>();
            IProvider Provider = BasaltCompilerBackends.GetCompilerBackend(Project, Args);

            switch (BinaryMeta.BinaryType)
            {
                case EBinaryType.Executable:
                    Provider.BuildExecutableTask();
                    break;
                case EBinaryType.Library:
                    Provider.BuildLibraryTask(BinaryMeta.LibraryType);
                    break;
            }

            Provider.Finish(Project);
        }

        BasaltCompilationDatabase.WriteFile();
    }

    private static BasaltLavaFile GetProjectFile(string[] Args)
    {
        foreach (string ProgramFlag in Args)
        {
            // Ignore compiler flags
            if (ProgramFlag.StartsWith('-')) continue;

            // Could be any name tbh
            if (ProgramFlag.EndsWith(BasaltLavaFile.Extension))
                return new(ProgramFlag);
        }
        return new("Build.lava");
    }

    public static BasaltProject GetProject(BasaltLavaFile ProjectFile)
    {
        BasaltLanguageLexer Lexer = new(ProjectFile);
        Lexer.Run();
        BasaltLanguageParser Parser = Lexer.UseParser();
        Parser.Run();
        return Parser.Project;
    }
}