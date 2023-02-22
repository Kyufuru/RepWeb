using System.Web.Mvc;

namespace 报表综合平台.Areas.Report
{
    /// <summary>
    /// 报表区域
    /// </summary>
    public class ReportAreaRegistration : AreaRegistration 
    {
        /// <summary>
        /// 区域名称
        /// </summary>
        public override string AreaName 
        {
            get 
            {
                return "Report";
            }
        }
        /// <summary>
        /// 注册区域
        /// </summary>
        /// <param name="context">上下文</param>
        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Report_default",
                "Report/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}