using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;

namespace 报表综合平台
{
    /// <summary>
    /// 
    /// </summary>
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        public override void Init() 
        { 
            PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest; 
            base.Init(); 
        }
        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e) 
        {
            HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required); 
        }
    }
}
