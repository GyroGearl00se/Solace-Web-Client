using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Solace_Web_Client.Pages
{
    public class PubSubModel : PageModel
    {
        private readonly ILogger<PubSubModel> _logger;

        public PubSubModel(ILogger<PubSubModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
