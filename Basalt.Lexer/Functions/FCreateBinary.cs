using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.LavaLang.Functions
{
    public enum EBinaryType
    {
        Executable,
        Library
    }

    public enum ELibraryType
    {
        Static,
        Dynamic,
        None
    }

    /**
     * BinaryType - could be Binary
     */
    public class FCreateBinary(EBinaryType BinaryType, ELibraryType LibraryType)
    {
        public bool IsLibrary
        {
            get
            {
                return BinaryType == EBinaryType.Library;
            }
        }

        public EBinaryType BinaryType { get; } = BinaryType;

        public ELibraryType LibraryType { get; } = LibraryType;
    }
}
