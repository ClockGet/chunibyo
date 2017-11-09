using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.ViewModels;
namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //ClaimsIdentity claimsIdentity = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "YS") }, "Basic");
            //ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            //await HttpContext.SignInAsync(claimsPrincipal);
            //await HttpContext.SignOutAsync();
            return View();
        }
        
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
