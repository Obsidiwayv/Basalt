using Basalt.LavaLang.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    // A block that contains other arrays inside it
    public class LavaBlockNode<T>(string KeyName)
        : INode<List<T>>, IPartialNode
    {
        public string Key { get => KeyName; }
        public List<T> Value { get; } = [];
        public ENodeEntityType Type { get; } = ENodeEntityType.Block;
    }
}
