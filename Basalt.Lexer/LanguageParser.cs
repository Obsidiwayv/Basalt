using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltLanguageParser(
        List<string> SyntaxMap, BasaltLavaFile ProjectFile, BasaltProject? Parent) 
        : IBasicLanguagePipeline<BasaltLanguageParser>
    {
        public BasaltLavaFile ProjectFile { get; } = ProjectFile;

        public List<IPartialNode> Nodes { get; } = [];

        public BasaltProject Project
        {
            get
            {
                return new(Nodes, ProjectFile, Parent);
            }
        }

        public BasaltLanguageParser Run() 
        {
            for (int Index = 0; Index < SyntaxMap.Count; Index++)
            {
                string Word = SyntaxMap[Index];

                // Attributes
                if (Word == "@" && SyntaxMap.Count - Index > 3)
                {
                    Nodes.Add(new LavaStringNode(
                        /* Attribute Name */ SyntaxMap[Index + 1],
                        /* Attribute Value */ SyntaxMap[Index + 2], ENodeEntityType.Attribute));
                }

                // Macros
                if (Word == "#" && SyntaxMap.Count - Index > 2)
                {
                    Nodes.Add(new LavaMacroNode(SyntaxMap[Index + 1]));
                }

                // Functions
                if (SyntaxMap.Count - Index > 1 && SyntaxMap[Index + 1] == "(")
                {
                    LavaFunctionNode FunctionNode = new(Word, []);
                    Index++;
                    Index++;
                    for (int SubIndex = 0 + Index; SubIndex < SyntaxMap.Count; SubIndex++)
                    {
                        string SubWord = SyntaxMap[SubIndex];
                        if (SubWord == ")")
                        {
                            Nodes.Add(FunctionNode);
                            break;
                        }
                        FunctionNode.Value.Add(SubWord);
                        Index++;
                    }
                }

                // Arrays
                if (SyntaxMap.Count - Index > 1 && SyntaxMap[Index+1] == "{" && SyntaxMap[Index] != ")" /* We cant have blocks counted here */)
                {
                    Index++;
                    Index++;
                    List<string> Strings = [];
                    LavaBlockNode<LavaConfigurationNode> ConfigNodes = new(Word);

                    int SubIndex = Index;
                    for (; SubIndex < SyntaxMap.Count; SubIndex++)
                    {
                        string SubWord = SyntaxMap[SubIndex];
                        if (SubWord.Contains("rules")) // its a rule configuration
                        {
                            LavaConfigurationNode Config = new(SubWord);
                            SubIndex++;
                            HandleOSConfigRules(SyntaxMap[SubIndex], (OS) =>
                            {
                                Config.Platform = OS;                     
                                SubIndex++;
                                SubIndex++;
                                
                                int RuleIndex = SubIndex;
                                for (; RuleIndex < SyntaxMap.Count; RuleIndex++)
                                {
                                    string FlagRule = SyntaxMap[RuleIndex];
                                    if (FlagRule == "}")
                                    {
                                        break;
                                    }
                                    Config.Value.Add(FlagRule);
                                }
                                SubIndex = RuleIndex ;
                            });
                            ConfigNodes.Value.Add(Config);
                            continue;
                        }
                        if (SubWord == "}")
                        {
                            if (Strings.Count != 0)
                            {
                                Nodes.Add(new LavaArrayNode(Word, Strings, ENodeEntityType.Array));
                            }
                            break;
                        }
                        Strings.Add(SubWord);
                        Index++;
                    }
                    if (ConfigNodes.Value.Count != 0)
                    {
                        Nodes.Add(ConfigNodes);
                    }
                    Index = SubIndex;
                }
            }
            return this;
        }
        private static void HandleOSConfigRules(string OSName, Action<OSPlatform> FN)
        {
            if (OSName == "macOS" && OperatingSystem.IsMacOS()) 
                FN.Invoke(OSPlatform.OSX);
        }
    }

}
