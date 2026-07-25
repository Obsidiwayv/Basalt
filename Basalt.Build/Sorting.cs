using Basalt.LavaLang;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.Build
{
    public class BasaltTopologicalSorting
    {
        private static HashSet<string> VisitedProjects { get; } = [];

        public static List<BasaltProject> BuildDepedencyGraph(
            BasaltProject Project)
        {
            LavaArrayNode? Dependencies = (LavaArrayNode?)Project.GetNode("Using");
            // if there was no dependency array then return the base project
            if (Dependencies == null) return [Project];

            List<BasaltProject> Graph = [];

            // ["lib1/build.lava", "lib2/build.lava"]
            foreach (string Dependent in Dependencies.Value)
            {
                BasaltProject ChildProject = BasaltBuildProgram.GetProject(
                    new BasaltLavaFile(Dependent), Project);
                Visit(ChildProject, Project, Graph);
            }

            Graph.Add(Project);
            return Graph;
        }

        private static void Visit(
            BasaltProject Project, 
            BasaltProject Parent,
            List<BasaltProject> GraphList)
        {
            // Skip the project if the hashset already contains it, so we dont build the project twice
            if (VisitedProjects.Contains(Project.FileSource.Name)) return;

            LavaArrayNode? Dependencies = (LavaArrayNode?)Project.GetNode("Using");
            if (Dependencies != null)
            {
                // Discovering more projects
                foreach (string Dependent in Dependencies.Value)
                {
                    BasaltProject ChildParser = BasaltBuildProgram.GetProject(
                        new BasaltLavaFile(Dependent), Parent);
                    Visit(ChildParser, Parent, GraphList);
                }
            }
            GraphList.Add(Project);
            VisitedProjects.Add(Project.FileSource.Name);
        }
    }
}
