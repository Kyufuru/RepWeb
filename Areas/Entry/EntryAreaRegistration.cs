using System.Web.Mvc;

namespace 报表综合平台.Areas.Entry
{
    /// <summary>
    /// 区域配置
    /// </summary>
    public class EntryAreaRegistration : AreaRegistration 
    {
        /// <summary>
        /// 区域名称
        /// </summary>
        public override string AreaName 
        {
            get 
            {
                return "Entry";
            }
        }
        /// <summary>
        /// 注册区域
        /// </summary>
        /// <param name="context">区域上下文</param>
        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Entry_default",
                "Entry/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}