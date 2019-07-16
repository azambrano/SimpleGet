using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NuGet.Versioning;

namespace SimpleGet.Core.Entities
{
    public interface IContext
    {
        DatabaseFacade Database { get; }

        DbSet<Package> Packages { get; set; }

        /// <summary>
        /// Check whether a <see cref="DbUpdateException"/> is due to a SQL unique constraint violation.
        /// </summary>
        /// <param name="exception">The exception to inspect.</param>
        /// <returns>Whether the exception was caused to SQL unique constraint violation.</returns>
        bool IsUniqueConstraintViolationException(DbUpdateException exception);

        /// <summary>
        /// Whether this database engine supports LINQ "Take" in subqueries.
        /// </summary>
        bool SupportsLimitInSubqueries { get; }

        Task<int> SaveChangesAsync();
    }

    public interface IDataBaseContext
    {
        Task<long> GetPackagesCountAsync();

        Task<bool> AddPackages(List<Package> packages);

        Task<bool> AddPackage(Package package);

        Task<bool> UpdatePackage(Package package);

        Task<bool> IsPackageAlreadyExists(string id, NuGetVersion version = null);

        Task<List<Package>> FindPackages(string id, bool includeUnlisted = false);

        Task<Package> FindPackage(string id, NuGetVersion version, bool includeUnlisted = false);

        Task<List<Package>> SearchPackageImplAsync(string query,
            int skip,
            int take,
            bool includePrerelease,
            bool includeSemVer2,
            string packageType,
            IReadOnlyList<string> frameworks);

        Task<IReadOnlyList<string>> GetAutocomplete(string query, int skip = 0, int take = 20);

        Task<IReadOnlyList<string>> FindPackagesDependents(string packageId, int skip = 0, int take = 20);

        Task<bool> HardDeletePackage(string id, NuGetVersion version);

        Task<List<Package>> GetBatch(int page, int pageMax);
    }
}
