using Basalt.LavaLang.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    public class LavaMacroNode(string Value) : INode<string>, IPartialNode
    {
        public string Key { get; } = "MACRO";

        public string Value { get; } = Value;

        public ENodeEntityType Type { get; } = ENodeEntityType.MacroToggle;
    }
}
