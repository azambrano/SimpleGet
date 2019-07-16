using System.Collections.Generic;
using SimpleGet.Extensions;
using SimpleGet.Protocol;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;

namespace SimpleGet.Controllers
{
    /// <summary>
    /// The NuGet Service Index. This aids NuGet client to discover this server's services.
    /// </summary>
    public class IndexController : Controller
    {
        private IEnumerable<ServiceIndexResource> BuildResource(string name, string url, params string[] versions) 
        {
            foreach (var version in versions) 
            {
                var type = string.IsNullOrEmpty(version) ? name : $"{name}/{version}";

                yield return new ServiceIndexResource(type, url);
            }
        }

        // GET v3/index
        [HttpGet]
        public ServiceIndex Get()
        {
            var resources = new List<ServiceIndexResource>();

            resources.AddRange(BuildResource("PackagePublish", Url.PackagePublish(), "2.0.0"));
            resources.AddRange(BuildResource("SymbolPackagePublish", Url.SymbolPublish(), "4.9.0"));
            resources.AddRange(BuildResource("SearchQueryService", Url.PackageSearch(), "", "3.0.0-beta", "3.0.0-rc"));
            resources.AddRange(BuildResource("RegistrationsBaseUrl", Url.RegistrationsBase(), "", "3.0.0-rc", "3.0.0-beta"));
            resources.AddRange(BuildResource("PackageBaseAddress", Url.PackageBase(), "3.0.0"));
            resources.AddRange(BuildResource("SearchAutocompleteService", Url.PackageAutocomplete(), "", "3.0.0-rc", "3.0.0-beta"));

            return new ServiceIndex(new NuGetVersion("3.0.0"), resources);
        }
    }
}