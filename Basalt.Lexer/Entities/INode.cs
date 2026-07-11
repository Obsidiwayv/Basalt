using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Entities
{
    internal interface INode<T>
    {
        string Key { get; }
        T Value { get; }
        ENodeEntityType Type { get; }
    }
}
