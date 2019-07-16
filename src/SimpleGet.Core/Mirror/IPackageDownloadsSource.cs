using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleGet.Core.Mirror
{
    public interface IPackageDownloadsSource
    {
        Task<Dictionary<string, Dictionary<string, long>>> GetPackageDownloadsAsync();
    }
}
