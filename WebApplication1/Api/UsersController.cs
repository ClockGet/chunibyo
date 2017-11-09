using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DAL;
using WebApplication1.Filters;
using WebApplication1.Helpers;
using WebApplication1.Services;
using WebApplication1.ViewModels;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication1.Api
{
    [Route("api/[controller]/[action]")]
    [ServiceFilter(typeof(ApiExceptionFilterAttribute))]
    public class UsersController : Controller
    {
        private MySqlContext dbContext;
        private EmailService _emailService;
        private TokenService _tokenService;
        public UsersController(MySqlContext context, EmailService emailService, TokenService tokenService)
        {
            dbContext = context;
            _emailService = emailService;
            _tokenService = tokenService;
        }
        [HttpPost]
        public async Task<IActionResult> Login(UserViewModel uvm)
        {
            if (string.IsNullOrEmpty(uvm.UserName) || string.IsNullOrEmpty(uvm.PassWord) || uvm.PassWord.Length != 32)
            {
                return Json(new { success = false, message = "用户名或密码不能为空" });
            }
            var user = await dbContext.UserDb.Where(p => p.UserName == uvm.UserName && p.PassWord == uvm.PassWord).FirstOrDefaultAsync();
            if (user == null)
                return Json(new { success = false, message = "用户名或密码不正确" });
            user.LoginDt = DateTime.Now;
            await dbContext.SaveChangesAsync();
            HttpContext.Session.Set("User", new UserViewModel { Id = user.UserId, UserName = user.UserName, PassWord = user.PassWord });
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel uvm)
        {
            if (string.IsNullOrEmpty(uvm.UserName) || string.IsNullOrEmpty(uvm.PassWord) || uvm.PassWord.Length != 32)
            {
                return Json(new { success = false, message = "用户名或密码不能为空" });
            }
            var user = await dbContext.UserDb.Where(p => p.UserName == uvm.UserName).FirstOrDefaultAsync();
            if (user != null)
                return Json(new { success = false, message = "用户名已经存在" });
            var time = DateTime.Now;
            var userModel = new Models.User { UserName = uvm.UserName, PassWord = uvm.PassWord, Dt = time, LoginDt = time };
            await dbContext.UserDb.AddAsync(userModel);
            await dbContext.SaveChangesAsync();
            HttpContext.Session.Set("User", userModel);
            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Json(new { success = true });
        }
        [HttpGet("{username}")]
        public async Task<IActionResult> Reset(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "邮箱地址不能为空" });
            var user = await dbContext.UserDb.Where(p => p.UserName == username).FirstOrDefaultAsync();
            if (user == null)
                return Json(new { success = false, message = "邮箱地址不存在" });
            string url = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/account/resetpassword?userId={user.UserId}&{_tokenService.Encrypt(JsonConvert.SerializeObject(new {id=user.UserId, userName=username, dt=DateTime.Now }))}";
            await _emailService.SendAsync(username, "请通过单击 <a href=\"" + url + "\">此处</a>来重置你的密码", "重置密码");
            return Json(new { success = true });
        }
    }
}
