using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.DAL;
using WebApplication1.Services;
using WebApplication1.ViewModels;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private MySqlContext _dbContext;
        private TokenService _tokenService;
        public AccountController(MySqlContext context, TokenService tokenService)
        {
            _dbContext = context;
            _tokenService = tokenService;
        }
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword(int userId, string token1, string token2)
        {
            if(userId==0 || string.IsNullOrEmpty(token1) || string.IsNullOrEmpty(token2))
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _dbContext.UserDb.Where(p => p.UserName == model.Email).FirstOrDefaultAsync();
            if (user == null || string.IsNullOrEmpty(model.Token1) || string.IsNullOrEmpty(model.Token2))
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var str = _tokenService.Decrypt(model.Token1, model.Token2);
            dynamic data = JsonConvert.DeserializeObject(str);
            if ((DateTime.Now - DateTime.Parse((string)data["dt"])).TotalDays>1.0)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            if (user.UserId != (int)data["id"] || user.UserName != (string)data["userName"])
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            using (MD5 md5 = MD5.Create())
            {
                byte[] msgBuffer = Encoding.UTF8.GetBytes(model.Password);
                byte[] md5Buffer = md5.ComputeHash(msgBuffer);
                md5.Clear();
                StringBuilder sbMd5 = new StringBuilder();
                for (int i = 0; i < md5Buffer.Length; i++)
                {
                    sbMd5.Append(md5Buffer[i].ToString("x2"));
                }
                user.PassWord = sbMd5.ToString();
            }
            try
            {
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
