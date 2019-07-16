using System.Threading.Tasks;

namespace SimpleGet.Core.Authentication
{
    public interface IAuthenticationService
    {
        Task<bool> AuthenticateAsync(string apiKey);
    }
}
