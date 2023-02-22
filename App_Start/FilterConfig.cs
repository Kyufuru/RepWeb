using System.Web;
using System.Web.Mvc;

namespace 报表综合平台
{
    /// <summary>
    /// 过滤规则
    /// </summary>
    public class FilterConfig
    {
        /// <summary>
        /// 注册过滤规则
        /// </summary>
        /// <param name="filters"></param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
