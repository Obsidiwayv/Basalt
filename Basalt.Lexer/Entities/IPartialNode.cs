using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Entities
{
    /**
     * An interface like <seealso cref="INode{T}"/> but missing the Value property with its generic
     */
    public interface IPartialNode
    {
        string Key { get; }
        ENodeEntityType Type { get; }
    }
}
