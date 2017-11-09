using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace WebApplication1
{
    public class Route
    {
        public static void Configure(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
