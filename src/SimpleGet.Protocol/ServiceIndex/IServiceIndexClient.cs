using System.Threading.Tasks;

namespace SimpleGet.Protocol
{
    public interface IServiceIndexClient
    {
        Task<ServiceIndex> GetServiceIndexAsync(string indexUrl);
    }
}
