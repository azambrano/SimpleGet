using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleGet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SimpleGet.Core.Mirror
{
    public class DownloadsImporter
    {
        private const int BatchSize = 200;

        //private readonly IContext _context;
        private readonly IDataBaseContext _dbContext;
        private readonly IPackageDownloadsSource _downloadsSource;
        private readonly ILogger<DownloadsImporter> _logger;

        public DownloadsImporter(
            //IContext context,
            IDataBaseContext dbContext,
            IPackageDownloadsSource downloadsSource,
            ILogger<DownloadsImporter> logger)
        {
            //_context = context ?? throw new ArgumentNullException(nameof(context));
            _downloadsSource = downloadsSource ?? throw new ArgumentNullException(nameof(downloadsSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task ImportAsync()
        {
            var packageDownloads = await _downloadsSource.GetPackageDownloadsAsync();
            var packages = await _dbContext.GetPackagesCountAsync();// _context.Packages.CountAsync();
            var batches = (packages / BatchSize) + 1;

            for (var batch = 0; batch < batches; batch++)
            {
                _logger.LogInformation("Importing batch {Batch}...", batch);

                var packagesToSave = new List<Package>();
                foreach (var package in await GetBatch(batch))
                {
                    var packageId = package.Id.ToLowerInvariant();
                    var packageVersion = package.VersionString.ToLowerInvariant();

                    if (!packageDownloads.ContainsKey(packageId) ||
                        !packageDownloads[packageId].ContainsKey(packageVersion))
                    {
                        continue;
                    }

                    package.Downloads = packageDownloads[packageId][packageVersion];
                    packagesToSave.Add(package);
                }

               await _dbContext.AddPackages(packagesToSave);

                _logger.LogInformation("Imported batch {Batch}", batch);
            }
        }

        private async Task<List<Package>> GetBatch(int page)
            => await _dbContext.GetBatch(page, BatchSize);
    }
}
