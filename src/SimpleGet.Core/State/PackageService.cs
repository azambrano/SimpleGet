using NuGet.Versioning;
using SimpleGet.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleGet.Core.State
{
    public class PackageService : IPackageService
    {
        private readonly IDataBaseContext _context;

        public PackageService(IDataBaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PackageAddResult> AddAsync(Package package)
        {
            if (!(await _context.IsPackageAlreadyExists(package.Id, package.Version)))
            {
                await _context.AddPackage(package);
                return PackageAddResult.Success;
            }
            return PackageAddResult.PackageAlreadyExists;
        }

        public async Task<bool> ExistsAsync(string id, NuGetVersion version = null)
        {
            return await _context.IsPackageAlreadyExists(id, version);
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted = false)
        {
            return (await _context.FindPackages(id, includeUnlisted)).AsReadOnly();
        }

        public async Task<Package> FindOrNullAsync(string id, NuGetVersion version, bool includeUnlisted = false)
        {
            return await _context.FindPackage(id, version, includeUnlisted);
        }

        public Task<bool> UnlistPackageAsync(string id, NuGetVersion version)
        {
            return TryUpdatePackageAsync(id, version, p => p.Listed = false);
        }

        public Task<bool> RelistPackageAsync(string id, NuGetVersion version)
        {
            return TryUpdatePackageAsync(id, version, p => p.Listed = true);
        }

        public Task<bool> AddDownloadAsync(string id, NuGetVersion version)
        {
            return TryUpdatePackageAsync(id, version, p => p.Downloads += 1);
        }

        public async Task<bool> HardDeletePackageAsync(string id, NuGetVersion version)
        {
            return await _context.HardDeletePackage(id, version);
        }

        private async Task<bool> TryUpdatePackageAsync(string id, NuGetVersion version, Action<Package> action)
        {
            var package = await _context.FindPackage(id, version);
            if (package != null)
            {
                action(package);
                return await _context.UpdatePackage(package);
            }

            return false;
        }
    }
}
