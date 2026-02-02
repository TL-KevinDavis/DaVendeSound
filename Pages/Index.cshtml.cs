using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DaVende_Sound.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly string _mediaBaseUrl;
        public string MediaBaseUrl => _mediaBaseUrl;

        public IndexModel(ILogger<IndexModel> logger, IOptions<MediaOptions> mediaOptions)
        {
            _logger = logger;
            _mediaBaseUrl = mediaOptions.Value.BaseUrl?.TrimEnd('/') + "/";
        }

        public void OnGet()
        {

        }
    }
}
