using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using Dapper;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json.Linq;
using 报表综合平台.Areas.Entry.Models;

namespace 报表综合平台.Areas.Entry.Controllers
{
    /// <summary>
    /// API
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class EntryController : ApiController
    {
        readonly string CONNSTR = ConfigurationManager.ConnectionStrings["中间层"].ConnectionString;
        readonly Dictionary<string, string> HREF = new Dictionary<string, string>
        {
            ["jumperjitindex.jumpergroup.cn"] = "jumperjitreport.jumpergroup.cn",
            ["61.145.74.81:38301"] = "61.145.74.81:38302",
            ["192.168.0.195:8088"] = "192.168.0.220:18301",
            ["localhost:52689"] = "192.168.0.220:18301"
        };
        readonly Dictionary<string, string> SQL = new Dictionary<string, string>
        {
            ["user"] = "SELECT * FROM J数据中心_用户表",
            ["rep"] = "SELECT * FROM J数据中心_功能表",
            ["auth"] = "SELECT * FROM J数据中心_权限表",
            ["stat"] = "SELECT * FROM J数据中心_统计表 ORDER BY 本地时间 DESC"
        };

        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <returns>所有数据</returns>
        [HttpGet]
        public IHttpActionResult GetAll()
        {
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                    return Json(new{
                        user = db.Query(SQL["user"]),
                        rep = db.Query(SQL["rep"]),
                        auth = db.Query(SQL["auth"]),
                        stat = db.Query(SQL["stat"])
                    });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 新建条目
        /// </summary>
        /// <param name="req">待新建条目</param>
        /// <returns>新列表</returns>
        [HttpPost]
        public IHttpActionResult Add([FromBody] JObject req)
        {
            string t = $"{req["type"]}";
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                {
                    switch (t)
                    {
                        case "user":
                            var user = req["data"].ToObject<J数据中心_用户表>();
                            db.Execute($@"
                                INSERT INTO J数据中心_用户表 
                                VALUES(LOWER(REPLACE(NEWID(),'-','')),'{user.用户}','{user.部门}')"
                            );
                            break;
                        case "rep":
                            db.Insert(req["data"].ToObject<J数据中心_功能表>());
                            break;
                        case "auth":
                            var auth = req["data"].ToObject<J数据中心_权限表>();
                            int uid = db.QuerySingle<J数据中心_用户表>($"SELECT ID FROM J数据中心_用户表 WHERE 用户='{auth.用户}'").ID;
                            var rids = db.Query<J数据中心_功能表>(
                                $"SELECT * FROM J数据中心_功能表 WHERE 报表名称 IN('{auth.报表名称.Replace(",","','")}')"
                            );
                            rids.ToList().ForEach(x =>
                            {
                                db.Insert(new J数据中心_权限表 { 
                                    用户ID = uid,
                                    报表ID = x.ID,
                                    用户 = auth.用户,
                                    报表名称 = x.报表名称
                                });
                            });
                            
                            break;
                        default:
                            return NotFound();
                    }
                    return Json(db.Query(SQL[t]));
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 删除条目
        /// </summary>
        /// <param name="req">待删除条目</param>
        /// <returns>新列表</returns>
        [HttpPost]
        public IHttpActionResult Del([FromBody] JObject req)
        {
            string ids, t = $"{req["type"]}";
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                {
                    switch (t)
                    {
                        case "user":
                            var user = new List<J数据中心_用户表>();
                            req["data"].Children().ToList().ForEach(
                                r => user.Add(r.ToObject<J数据中心_用户表>())
                            );
                            db.Delete(user);
                            ids = string.Join(",", user.Select(x => x.ID));
                            db.Execute($"DELETE FROM J数据中心_权限表 WHERE 用户ID IN({ids})");
                            break;
                        case "rep":
                            var rep = new List<J数据中心_功能表>();
                            req["data"].Children().ToList().ForEach(
                                r => rep.Add(r.ToObject<J数据中心_功能表>())
                            );
                            db.Delete(rep);
                            ids = string.Join(",", rep.Select(x => x.ID));
                            db.Execute($"DELETE FROM J数据中心_权限表 WHERE 报表ID IN({ids})");
                            break;
                        case "auth":
                            var auth = new List<J数据中心_权限表>();
                            req["data"].Children().ToList().ForEach(
                                r => auth.Add(r.ToObject<J数据中心_权限表>())
                            );
                            db.Delete(auth);
                            break;
                        case "stat":
                            var stat = new List<J数据中心_统计表>();
                            req["data"].Children().ToList().ForEach(
                                r => stat.Add(r.ToObject<J数据中心_统计表>())
                            );
                            db.Delete(stat);
                            break;
                        default:
                            return NotFound();
                    }
                    return Json(db.Query(SQL[t]));
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 修改条目
        /// </summary>
        /// <param name="req">待修改条目</param>
        /// <returns>新列表</returns>
        [HttpPost]
        public IHttpActionResult Edit([FromBody] JObject req)
        {
            string t = $"{req["type"]}";
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                {
                    switch (t)
                    {
                        case "user":
                            var user = req["data"].ToObject<J数据中心_用户表>();
                            db.Update(user);
                            db.Execute($@"UPDATE J数据中心_权限表 SET 用户 = '{user.用户}' WHERE 用户ID = {user.ID}");
                            break;
                        case "rep":
                            var rep = req["data"].ToObject<J数据中心_功能表>();
                            db.Update(rep);
                            db.Execute($"UPDATE J数据中心_权限表 SET 报表名称 = '{rep.报表名称}' WHERE 报表ID = {rep.ID}");
                            break;
                        case "auth":
                            var auth = req["data"].ToObject<J数据中心_权限表>();
                            db.Update(auth);
                            break;
                        default:
                            return NotFound();
                    }
                    return Json(db.Query(SQL[t]));
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 复制权限
        /// </summary>
        /// <param name="req">请求</param>
        /// <returns>新列表</returns>
        [HttpPost]
        public IHttpActionResult Copy([FromBody] JObject req)
        {
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                {
                    string from = $"{req["data"]["用户"]}";
                    string copy = $"{req["data"]["复制"]}";
                    int id = db.QuerySingle($"SELECT ID FROM J数据中心_用户表 WHERE 用户 = '{from}'").ID;
                    db.Execute($@"
                        DELETE FROM J数据中心_权限表 WHERE 用户 = '{from}';
                        INSERT INTO J数据中心_权限表
                        SELECT {id} 用户ID, 报表ID, '{from}' 用户, 报表名称
                        FROM J数据中心_权限表 WHERE 用户 = '{copy}'");
                    return Json(db.Query(SQL["auth"]));
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 获取用户信息及报表列表
        /// </summary>
        /// <param name="uuid">用户UUID</param>
        /// <param name="url">地址映射</param>
        /// <returns>入口报表列表</returns>
        [HttpGet]
        public IHttpActionResult GetUserReps(string uuid, string url)
        {
            uuid = HttpUtility.UrlDecode(uuid);
            string sqlUser = $"SELECT * FROM J数据中心_用户表 WHERE UUID = '{uuid}'";
            string sqlRep = $@"
                SELECT * FROM J数据中心_功能表 WHERE ID IN (
                    SELECT 报表ID FROM J数据中心_权限表 WHERE 用户ID IN (
                        SELECT ID FROM J数据中心_用户表 WHERE UUID = '{uuid}'
                    )
                ) ORDER BY 报表类别";
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                    return Json(new { 
                        user = db.QuerySingle(sqlUser),  
                        rep = db.Query(sqlRep), 
                        href = HREF[HttpUtility.HtmlDecode(url)] 
                    });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 更新链接统计信息
        /// </summary>
        /// <returns>更新结果</returns>
        [HttpPost]
        public IHttpActionResult AddStat([FromBody]J数据中心_统计表 req)
        {
            string ip = HttpContext.Current.Request.UserHostAddress;
            if ( ip == "::1" || ip.StartsWith("10.10.141")) return Ok();
            string sqlUser = $"SELECT ID,用户 FROM 统计类报表.dbo.J数据中心_用户表 WHERE 用户 = '{req.用户}'";
            string sqlRep = $"SELECT ID,报表名称 FROM 统计类报表.dbo.J数据中心_功能表 WHERE 报表名称 = '{req.报表名称}'";
            try
            {
                using (var db = new SqlConnection(CONNSTR))
                {
                    req.IP = ip;
                    req.用户ID = db.QuerySingle<J数据中心_用户表>(sqlUser).ID;
                    req.报表ID = db.QuerySingle<J数据中心_功能表>(sqlRep).ID;
                    req.服务器时间 = DateTime.Now;
                    db.Insert(req);
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}