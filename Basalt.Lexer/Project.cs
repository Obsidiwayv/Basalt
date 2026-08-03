using Basalt.LavaLang.Entities;
using Basalt.LavaLang.Impl;
using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang
{
    public class BasaltProject(
        List<IPartialNode> Nodes, BasaltLavaFile ProjectFile, BasaltProject? Parent = null)
    {
        public BasaltLavaFile FileSource { get; } = ProjectFile;

        public BasaltProject? ParentProject { get; } = Parent;

        public List<IPartialNode> Nodes { get; } = Nodes;

        // PartialNode can be used with instanceof
        public IPartialNode? GetNode(string Key)
        {
            int NodeCount = Nodes.Count(N => N.Key == Key);
            if (NodeCount > 1)
                throw new BasaltException($"More than one Key! has: {NodeCount}, Searching: %r{Key}%c");

            foreach (IPartialNode Node in Nodes)
            {
                if (Node.Key == Key) return Node;
            }
            return null;
        }

        public bool MacroIsPresent(string MacroName)
        {
            foreach (IPartialNode Node in Nodes)
            {
                if (Node.Type == ENodeEntityType.MacroToggle)
                {
                    LavaMacroNode Macro = (LavaMacroNode)Node;
                    if (Macro.Value == MacroName) return true;
                }
            }
            return false;
        }
    }
}
