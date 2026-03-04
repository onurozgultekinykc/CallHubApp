using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CallHubApp.Models;

namespace CallHubApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly List<ServerConfig> _servers;
        private readonly IWebHostEnvironment _env; // ortam bilgisi

        public HomeController(
            ILogger<HomeController> logger,
            List<ServerConfig> servers,
            IWebHostEnvironment env
        )
        {
            _logger = logger;
            _servers = servers;
            _env = env;
        }

        public IActionResult Index()
        {
            string ip = GetIpForEnvironment();
            return View(model: ip);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetIpForEnvironment()
        {
            if (_env.IsDevelopment())
            {
                // Development ortamý: local IP bul
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "IP bulunamadý";
            }
            else
            {
                // Production ortamý: sabit public IP
                return "89.252.185.202";
            }
        }
    }
}
