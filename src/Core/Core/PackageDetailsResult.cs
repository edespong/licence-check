using Serilog;

namespace LicenseInspector
{
    public enum PackageDetailsResultEnum
    {
        Success = 1,
        NoPackageFound,
        InternalPackage
    }

    public struct PackageDetailsResult
    {
        public PackageDetailsResultEnum Status { get; }

        public IPackageDetails? Package { get; }

        public PackageDetailsResult(IPackageDetails package)
        {
            Status = PackageDetailsResultEnum.Success;
            Package = package;
        }

        public PackageDetailsResult(PackageDetailsResultEnum status)
        {
            if (status == PackageDetailsResultEnum.Success)
            {
                Log.Error("A successful result shoud never be created without a package details");
            }

            Status = status;
            Package = null;
        }
    }
}
