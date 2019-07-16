using SimpleGet.Core.Entities;
using SimpleGet.Core.Indexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleGet.Core.Search
{
    public class DatabaseSearchService : ISearchService
    {
        private readonly IDataBaseContext _context;
        private readonly IFrameworkCompatibilityService _frameworks;

        public DatabaseSearchService(IDataBaseContext context, IFrameworkCompatibilityService frameworks)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _frameworks = frameworks ?? throw new ArgumentNullException(nameof(frameworks));
        }

        public Task IndexAsync(Package package) => Task.CompletedTask;

        public async Task<IReadOnlyList<SearchResult>> SearchAsync(
            string query,
            int skip = 0,
            int take = 20,
            bool includePrerelease = true,
            bool includeSemVer2 = true,
            string packageType = null,
            string framework = null)
        {
            var result = new List<SearchResult>();
            var frameworks = GetCompatibleFrameworks(framework);
            var packages = await SearchImplAsync(query, skip, take, includePrerelease, includeSemVer2, packageType, frameworks);

            foreach (var package in packages)
            {
                var versions = package.OrderByDescending(p => p.Version).ToList();
                var latest = versions.First();

                var versionResults = versions.Select(p => new SearchResultVersion(p.Version, p.Downloads));

                result.Add(new SearchResult
                {
                    Id = latest.Id,
                    Version = latest.Version,
                    Description = latest.Description,
                    Authors = latest.Authors,
                    IconUrl = latest.IconUrlString,
                    LicenseUrl = latest.LicenseUrlString,
                    ProjectUrl = latest.ProjectUrlString,
                    Summary = latest.Summary,
                    Tags = latest.Tags,
                    Title = latest.Title,
                    TotalDownloads = versions.Sum(p => p.Downloads),
                    Versions = versionResults.ToList().AsReadOnly(),
                });
            }

            return result.AsReadOnly();
        }

        private IReadOnlyList<string> GetCompatibleFrameworks(string framework)
        {
            if (framework == null) return null;

            return _frameworks.FindAllCompatibleFrameworks(framework);
        }

        private async Task<List<IGrouping<string, Package>>> SearchImplAsync(
            string query,
            int skip,
            int take,
            bool includePrerelease,
            bool includeSemVer2,
            string packageType,
            IReadOnlyList<string> frameworks)
        {
            return (await _context.SearchPackageImplAsync(query, skip, take, includePrerelease, includeSemVer2, packageType, frameworks))
                                   .GroupBy(x => x.Id).ToList();
        }

        public async Task<IReadOnlyList<string>> AutocompleteAsync(string query, int skip = 0, int take = 20)
        {
            return await _context.GetAutocomplete(query, skip, take);
        }

        public async Task<IReadOnlyList<string>> FindDependentsAsync(string packageId, int skip = 0, int take = 20)
        {
            return await _context.FindPackagesDependents(packageId, skip, take);
        }
    }
}
