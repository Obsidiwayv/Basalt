using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltLanguageParser(List<string> SyntaxMap, BasaltLavaFile ProjectFile) : IBasicLanguagePipeline
    {
        public BasaltLavaFile ProjectFile { get; } = ProjectFile;

        public List<IPartialNode> Nodes { get; } = [];

        public BasaltProject Project
        {
            get
            {
                return new(Nodes, ProjectFile);
            }
        }

        public void Run() 
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
                    for (int SubIndex = 0 + Index; SubIndex < SyntaxMap.Count; SubIndex++)
                    {
                        string SubWord = SyntaxMap[SubIndex];
                        if (SubWord == "}")
                        {
                            Nodes.Add(new LavaArrayNode(Word, Strings, ENodeEntityType.Array));
                            break;
                        }
                        Strings.Add(SubWord);
                        Index++;
                    }
                }
            }
        }
    }
}
