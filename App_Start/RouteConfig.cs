using System.Web.Mvc;
using System.Web.Routing;

namespace 报表综合平台
{
    /// <summary>
    /// 路由配置
    /// </summary>
    public class RouteConfig
    {
        /// <summary>
        /// 注册路由配置
        /// </summary>
        /// <param name="routes">路由表</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" },
                namespaces: new string[] { "报表综合平台.Controllers" } 
            );
        }
    }
}
