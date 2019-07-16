using System.Threading.Tasks;

namespace SimpleGet.Protocol
{
    public interface ISearchClient
    {
        Task<SearchResponse> GetSearchResultsAsync(string searchUrl);

        Task<AutocompleteResult> GetAutocompleteResultsAsync(string searchUrl);
    }
}
