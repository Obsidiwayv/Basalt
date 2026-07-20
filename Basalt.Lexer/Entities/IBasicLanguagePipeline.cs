using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Entities
{
    internal interface IBasicLanguagePipeline<T>
    {
        public T Run();
    }
}
