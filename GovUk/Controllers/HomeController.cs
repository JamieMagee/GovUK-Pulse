using Microsoft.AspNet.Mvc;
using Microsoft.WindowsAzure.Storage;

namespace GovUk.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private static string _lastModified = null;

        public IActionResult Index()
        {
            ViewData["LastModified"] = _lastModified ?? LastModified();
            return View();
        }
        
        [Route("about")]
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }

        private static string LastModified()
        {
            var connectionString = Startup.Configuration["Data:ConnectionString"];
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("scans");
            var blockBlob = container.GetBlockBlobReference("latest.json");
            blockBlob.FetchAttributes();
            if (blockBlob.Properties.LastModified != null)
                _lastModified = blockBlob.Properties.LastModified.Value.Date.ToString("M");
            return _lastModified;
        }
    }
}