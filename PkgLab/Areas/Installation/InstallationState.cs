using NuGet;

namespace PkgLab.Areas.Installation
{
    public class InstallationState
    {
        public IPackage Installed { get; set; }

        public IPackage Update { get; set; }
    }
}