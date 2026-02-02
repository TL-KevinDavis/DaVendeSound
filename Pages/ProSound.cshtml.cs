using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace DaVende_Sound.Pages
{
    public class ProSoundModel : PageModel
    {
        private readonly string _mediaBaseUrl;
        public string MediaBaseUrl => _mediaBaseUrl;

        public ProSoundModel(IOptions<MediaOptions> mediaOptions)
        {
            _mediaBaseUrl = mediaOptions.Value.BaseUrl?.TrimEnd('/') + "/";
        }

        public void OnGet()
        {
        }
    }
}
