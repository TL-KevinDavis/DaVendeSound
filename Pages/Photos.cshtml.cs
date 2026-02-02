using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DaVende_Sound.Pages
{
    public class PhotosModel : PageModel
    {
        private readonly ILogger<PhotosModel> _logger;
        private readonly string _mediaBaseUrl;
        public string MediaBaseUrl => _mediaBaseUrl;

        public PhotosModel(ILogger<PhotosModel> logger, IOptions<MediaOptions> mediaOptions)
        {
            _logger = logger;
            _mediaBaseUrl = mediaOptions.Value.BaseUrl?.TrimEnd('/') + "/";
        }

        public void OnGet()
        {
        }
    }

}