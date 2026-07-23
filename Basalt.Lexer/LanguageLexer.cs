using Basalt.LavaLang.Entities;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltLanguageLexer(BasaltLavaFile File)
        : IBasicLanguagePipeline<BasaltLanguageLexer>
    {
        public List<string> SyntaxMap { get; } = [];

        public BasaltLanguageLexer Run()
        {
            char[] FileContent = File.Fetch().ToCharArray();
            Read(FileContent);
            return this;
        }

        /**
         * Creates a new parser instance, runs, and then returns it
         */
        public BasaltLanguageParser PipeIntoParser(BasaltProject? Parent)
        {
            return new BasaltLanguageParser(SyntaxMap, File, Parent);
        }

        private void Read(char[] Contents)
        {
            StringBuilder Word = new();
            for (int Index = 0; Index < Contents.Length; Index++)
            {
                char C = Contents[Index];

                // Functions
                if (C == '(')
                {
                    // Make sure to clear the word before parsing the params
                    SyntaxMap.Add(Word.ToString());
                    Word.Clear();

                    StringBuilder SubString = new();
                    int Params = 0;
                    SyntaxMap.Add("(");
                    Index++;
                    for (int SubIndex = 0 + Index; SubIndex < Contents.Length; SubIndex++)
                    {
                        char CI = Contents[SubIndex];
                        if (CI == ')')
                        {
                            if (SubString.Length != 0)
                            {
                                Params++;
                                SyntaxMap.Add(SubString.ToString());
                            }
                            // Due to how the lexer parses characters this is the only safe way to parse a function
                            if (Params == 0 || Params == 1)
                                SyntaxMap.Add(")");
                            break;
                        }
                        if (CI == ',')
                        {
                            Params++;
                            SyntaxMap.Add(SubString.ToString());
                            SubString.Clear();
                            continue;
                        }
                        // skip all whitespaces
                        if (CI != ' ')
                        {
                            SubString.Append(CI);
                        }
                        Index++;
                    }
                    continue;
                }

                // Strings
                if (C == '"')
                {
                    StringBuilder SubString = new();
                    Index++;
                    for (int SubIndex = 0 + Index; SubIndex < Contents.Length; SubIndex++)
                    {
                        char CI = Contents[SubIndex];
                        if (CI == '"')
                        {
                            SyntaxMap.Add(SubString.ToString());
                            break;
                        }
                        SubString.Append(CI);
                        Index++;
                    }
                    continue;
                }

                // Make sure that all symbols are included
                if (char.IsPunctuation(C))
                {
                    SyntaxMap.Add(C.ToString());
                    continue;
                }

                if (C == ' ' || C == '\n')
                {
                    if (!string.IsNullOrWhiteSpace(Word.ToString()))
                    {
                        SyntaxMap.Add(Word.ToString());
                    }
                    Word.Clear();
                }
                else
                {
                    Word.Append(C);
                }
            }
        }
    }
}
