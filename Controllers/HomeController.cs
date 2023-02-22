using System.Web;
using System.Web.Mvc;

namespace 报表综合平台.Controllers
{
    /// <summary>
    /// 首页
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// 首页
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View("~/Areas/Entry/Views/Home.cshtml");
        }

        /// <summary>
        /// 后台
        /// </summary>
        /// <returns></returns>
        public ActionResult Manage()
        {
            return View("~/Areas/Entry/Views/Manage.cshtml");
        }

        /// <summary>
        /// 报表
        /// </summary>
        /// <param name="name">报表名称</param>
        /// <returns></returns>
        public ActionResult Report(string name)
        {
            string rep;
            switch (name)
            {
                case "新能采购进度表":
                    rep = "PurchaseProgressXN";
                    break;
                default:
                    return HttpNotFound();
            }
            return View($"~/Areas/Report/Views/{rep}.cshtml");
        }
    }
}