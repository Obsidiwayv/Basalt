using Basalt.Tile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Functions
{
    public class LavaFunctionParams
    {
        public static ELibraryType GetLibraryType(string LibraryParam)
        {
            return LibraryParam.ToLower() switch
            {
                "static" => ELibraryType.Static,
                "dynamic" => ELibraryType.Dynamic,
                _ => throw new BasaltException("Unknown library type in CreateBinary(binary_type, %mlibrary_type%c)")
            };
        }

        public static EBinaryType GetBinaryType(string BinaryTypeParam)
        {
            return BinaryTypeParam.ToLower() switch
            {
                "executable" => EBinaryType.Executable,
                "library" => EBinaryType.Library,
                _ => throw new BasaltException("Unknown binary type in CreateBinary(%mbinary_type%c, library_type)")
            };
        }
    }
}
