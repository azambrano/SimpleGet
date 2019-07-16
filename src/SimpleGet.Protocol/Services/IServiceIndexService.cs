using System.Threading.Tasks;

namespace SimpleGet.Protocol
{
    public interface IServiceIndexService
    {
        Task<string> GetPackageContentUrlAsync();

        Task<string> GetRegistrationUrlAsync();

        Task<string> GetSearchUrlAsync();
    }
}
