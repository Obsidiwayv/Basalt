using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes
{
    public class FileURL
    {
        /**
         * Wraps a url with ' if it contains spaces so it can get executed
         */
        public static string SafeWrap(string PossiblyUnstableURL)
        {
            if (PossiblyUnstableURL.Contains(' '))
            {
                return $"'{PossiblyUnstableURL}'";
            }
            return PossiblyUnstableURL;
        }
    }
}
