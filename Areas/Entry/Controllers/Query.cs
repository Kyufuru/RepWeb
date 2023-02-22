using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace 报表综合平台.Areas.Entry.Controllers
{
    /// <summary>
    /// 查询助手
    /// </summary>
    public class Query
    {
        /// <summary>
        /// 格式化查询字符串
        /// </summary>
        /// <param name="q">查询条件对象</param>
        /// <returns></returns>
        public static string GetQueryString(object q)
        {
            List<string> lq = new List<string>();
            string sq;

            foreach (var p in q.GetType().GetProperties())
            {
                var v = q.GetType().GetProperty(p.Name).GetValue(q);
                if (v == null || v.ToString() == "0") continue;
                if (v.GetType().IsValueType) sq = "{0}={1}";
                else if (v.GetType() == typeof(string)) sq = "{0}='{1}'";
                else continue;

                lq.Add(string.Format(sq, p.Name, v));
            }

            if (lq.Count == 0) return "";
            return " WHERE " + string.Join(" AND ", lq);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static string GetQueryString(Dictionary<string,object> q)
        {
            List<string> lq = new List<string>();
            string sq;

            foreach (var k in q.Keys)
            {
                var v = q[k];
                if (v == null || v.ToString() == "" || v.ToString() == "0") continue;
                if (v.GetType().IsValueType) sq = "{0}={1}";
                else if (v.GetType() == typeof(string)) sq = "{0}='{1}'";
                else continue;

                lq.Add(string.Format(sq, k, v));
            }

            if (lq.Count == 0) return "";
            return " WHERE " + string.Join(" AND ", lq);
        }
    }
}