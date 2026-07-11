using Basalt.LavaLang.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    public class LavaArrayNode(string KeyName, List<string> ValueWithType, ENodeEntityType EntityType)
        : INode<List<string>>, IPartialNode
    {
        public string Key { get => KeyName; }
        public List<string> Value { get => ValueWithType; }
        public ENodeEntityType Type { get => EntityType;  }
    }
}
