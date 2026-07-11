using Basalt.LavaLang.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Impl
{
    public class LavaStringNode(string KeyName, string ValueWithType, ENodeEntityType EntityType)
        : INode<string>, IPartialNode
    {
        public string Key { get => KeyName; }
        public string Value { get => ValueWithType; }
        public ENodeEntityType Type { get => EntityType; }
    }
}
