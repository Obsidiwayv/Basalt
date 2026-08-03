using System;
using System.Collections.Generic;
using System.Text;

namespace Basalt.BackendPipes.MSVC
{
    public class MSVCInstallation(string Edition, string InstalledVersion)
    {
        public string Edition { get; } = Edition;
        public string Version { get; } = InstalledVersion;
        public string FullPath { get; } = Path.Join(
            MSVCUtil.BaseVSDir, InstalledVersion, Edition);
    }
}
