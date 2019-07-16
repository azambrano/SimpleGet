using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NuGet.Versioning;
using SimpleGet.Core.Configuration;
using SimpleGet.Core.Entities;
using SimpleGet.Core.Indexing;
using SimpleGet.Core.Search;

namespace SimpleGet.DataBase.Mongo
{
    public class MongoDatabaseContext : IDataBaseContext
    {
        private readonly IMongoCollection<Package> packageDocument;

        public MongoDatabaseContext(IOptions<DatabaseOptions> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            var db = client.GetDatabase("nuget-data");
            packageDocument = db.GetCollection<Package>("packages");
        }

        public async Task<Package> FindPackage(string id, NuGetVersion version, bool includeUnlisted = false)
        {
            var search = includeUnlisted ? Builders<Package>.Filter.Where(x => x.Id == id && x.VersionString == version.ToNormalizedString()) :
                                          Builders<Package>.Filter.Where(x => x.Id == id && x.VersionString == version.ToNormalizedString() && x.Listed);

            var rawQuery = await packageDocument.Find(search).FirstOrDefaultAsync();
            return rawQuery;
        }

        public async Task<List<Package>> FindPackages(string packageId, bool includeUnlisted = false)
        {
            var search = includeUnlisted ? Builders<Package>.Filter.Where(x => x.Id == packageId) :
                                           Builders<Package>.Filter.Where(x => x.Id == packageId && x.Listed);

            var rawQuery = await packageDocument.Find(search).ToListAsync();
            return rawQuery.ToList();
        }

        public async Task<IReadOnlyList<string>> FindPackagesDependents(string packageId, int skip = 0, int take = 20)
        {
            var search = Builders<Package>.Filter.Where(x => x.Listed && x.Id == packageId);

            var rawQuery = await packageDocument.Find(search)
                    .SortByDescending(x => x.Downloads)
                    .Skip(skip).Limit(take)
                    .Project<Package>(Builders<Package>.Projection.Include(p => p.Id))
                    .ToListAsync();

            return rawQuery.Select(x => x.Id).ToList();
        }

        public async Task<IReadOnlyList<string>> GetAutocomplete(string query, int skip = 0, int take = 20)
        {
            var search = Builders<Package>.Filter.Where(x => x.Listed);
            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => p.Id.ToLower().Contains(query)));
            }
            var rawQuery = await packageDocument.Find(search)
                    .SortByDescending(x => x.Downloads)
                    .Skip(skip).Limit(take)
                    .Project<Package>(Builders<Package>.Projection.Include(p => p.Id))
                    .ToListAsync();

            return rawQuery.Select(x => x.Id).ToList();
        }

        public async Task<List<Package>> GetBatch(int page, int pageMax)
        {
            var rawQuery = await packageDocument.Find(Builders<Package>.Filter.Empty)
                .SortBy(x => x.Key)
                .Skip(page * pageMax)
                .Limit(pageMax)
                .ToListAsync();
            return rawQuery;
        }

        public async Task<long> GetPackagesCountAsync()
        {
            return await packageDocument.CountDocumentsAsync(Builders<Package>.Filter.Empty);
        }

        public async Task<bool> HardDeletePackage(string id, NuGetVersion version)
        {
            await packageDocument.DeleteOneAsync(p => p.Id == id && p.VersionString == version.ToNormalizedString());
            return true;
        }

        public async Task<bool> IsPackageAlreadyExists(string id, NuGetVersion version = null)
        {
            var query = version != null
                ? await packageDocument.CountDocumentsAsync(p => p.Id == id && p.VersionString == version.ToNormalizedString())
                : await packageDocument.CountDocumentsAsync(p => p.Id == id);
            return query > 0;
        }

        public async Task<bool> AddPackage(Package package)
        {
            await packageDocument.InsertOneAsync(package);
            return true;
        }

        public async Task<bool> UpdatePackage(Package package)
        {
            await packageDocument.ReplaceOneAsync(Builders<Package>.Filter.Where(x => x.Id == package.Id && x.VersionString == package.Version.ToNormalizedString()), package);
            return true;
        }

        public async Task<bool> AddPackages(List<Package> packages)
        {
            await packageDocument.InsertManyAsync(packages);
            return true;
        }

        public async Task<List<Package>> SearchPackageImplAsync(string query, int skip, int take, bool includePrerelease, bool includeSemVer2, string packageType, IReadOnlyList<string> frameworks)
        {
            var search = Builders<Package>.Filter.Where(x => x.Listed);

            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => p.Id.ToLower().Contains(query)));
            }

            if (!includePrerelease)
            {
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => !p.IsPrerelease));
            }

            if (!includeSemVer2)
            {
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => p.SemVerLevel != SemVerLevel.SemVer2));
            }

            if (!string.IsNullOrEmpty(packageType))
            {
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => p.PackageTypes.Any(t => t.Name == packageType)));
            }

            if (frameworks != null)
            {
                search = Builders<Package>.Filter.And(search, Builders<Package>.Filter.Where(p => p.TargetFrameworks.Any(f => frameworks.Contains(f.Moniker))));
            }

            var rawQuery = await packageDocument.Find(search)
                                            .Project<Package>(Builders<Package>.Projection.Include(p => p.Id))
                                            .Sort(Builders<Package>.Sort.Descending(x => x.Id))
                                            //.Distinct()
                                            .Skip(skip)
                                            .Limit(take)
                                            .ToListAsync();

            var packageIds = rawQuery.Select(x => x.Id).ToList();

            //// This query MUST fetch all versions for each package that matches the search,
            //// otherwise the results for a package's latest version may be incorrect.
            //// If possible, we'll find all these packages in a single query by matching
            //// the package IDs in a subquery. Otherwise, run two queries:
            ////   1. Find the package IDs that match the search           

            var response = await packageDocument.Find(Builders<Package>.Filter.In(x => x.Id, packageIds)).ToListAsync();
            return response;
        }
    }
}
