using Basalt.LavaLang.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    // A block that contains other arrays inside it
    public class LavaConfigurationNode(string KeyName)
        : INode<List<string>>, IPartialNode
    {
        public string Key { get => KeyName; }
        public List<string> Value { get; } = [];
        public OSPlatform Platform { get; set; }
        public ENodeEntityType Type { get; } = ENodeEntityType.Array;
    }
}
