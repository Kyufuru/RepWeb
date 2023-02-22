using System.Configuration;
using System.Web.Http;
using System.Web.Http.Cors;

namespace 报表综合平台
{
    /// <summary>
    /// Web API 配置
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// 注册 Web API
        /// </summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // 允许跨域访问
            config.EnableCors();

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
