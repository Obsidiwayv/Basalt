using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.Tile
{
    public class BasaltException(string Message) 
        : Exception(BasaltLogger.ParseColorOutput(Message))
    {}
}
