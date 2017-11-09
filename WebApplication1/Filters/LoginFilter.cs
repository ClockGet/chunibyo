using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DAL;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Filters
{
    public class LoginFilter : IAsyncActionFilter
    {
        private MySqlContext _context;
        public LoginFilter(MySqlContext context)
        {
            _context = context;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            bool signedIn = false;
            if(context.HttpContext.Session.Get<UserViewModel>("User")==null)
            {
                string cookies = context.HttpContext.Request.Cookies["user"];
                if(!string.IsNullOrEmpty(cookies))
                {
                    dynamic data = JsonConvert.DeserializeObject(cookies);
                    if (data != null)
                    {
                        string userName = data["userName"];
                        string passWord = data["passWord"];
                        var r = await (from c in _context.UserDb where c.UserName == userName && c.PassWord == passWord select c).FirstOrDefaultAsync();
                        if (r != null)
                        {
                            signedIn = true;
                            context.HttpContext.Session.Set("User", new UserViewModel { Id = r.UserId, UserName = r.UserName, PassWord = r.PassWord });
                        }
                    }
                }
            }
            else
            {
                signedIn = true;
            }
            if (!signedIn)
                context.HttpContext.Response.StatusCode = 401;
            else
                await next();
        }
    }
}
