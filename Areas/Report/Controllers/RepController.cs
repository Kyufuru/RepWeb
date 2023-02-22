using Dapper;
using Dapper.Contrib.Extensions;
using ExcelDataReader;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using 报表综合平台.Areas.Report.Models;

namespace 报表综合平台.Areas.Report.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class RepController : ApiController
    {
        readonly static string CONNSTR_CE = ConfigurationManager.ConnectionStrings["中间层"].ConnectionString;
        readonly static string CONNSTR_IMG = ConfigurationManager.ConnectionStrings["中间层镜像"].ConnectionString;
        public static Dictionary<string, object> session = new Dictionary<string, object>();

        static readonly Regex regPNo = new Regex(@".*\d{2}", RegexOptions.Compiled);
        static readonly Regex regBnk = new Regex(@"\s+", RegexOptions.Compiled);
        static readonly Regex regCol = new Regex(@"类别|图纸|材料", RegexOptions.Compiled);
        static readonly Regex regSep = new Regex(@"(及|包括|等|部分|[\s,.\(\)，、。（）])+", RegexOptions.Compiled);
        static readonly string sqlCus = @"
        SELECT 名称 FROM 客户表
        WHERE ID IN(
            SELECT 客户ID FROM 工程表
            WHERE 组织ID = 100458 
            AND 名称 = '{0}'
        )";
        static readonly string sqlBom = @"
        SELECT
            产品名称,
            COUNT(*) 单据数量,
            CASE WHEN SUM(IIF(下达日期 IS NULL,1,0)) = 0
                THEN MAX(下达日期)
            END 图纸下发实际日期,
            CASE WHEN SUM(IIF(实收数量 < 允许采购数量 OR 实收数量 IS NULL,1,0)) = 0
                THEN MAX(审核日期)
            END 材料到货实际日期,
            SUM(IIF(行业务关闭 = 'B',1,0)) 订单完成数量,
            SUM(IIF(实收数量 >= 允许采购数量,1,0)) 到货完成数量
        FROM (
            SELECT 
                ID,
                物料名称 产品名称
            FROM 物料表 
            WHERE 物料名称 NOT LIKE '（禁用）%'
        ) A1
        LEFT JOIN (
                SELECT
                    产品ID,
                    工程ID,
                    子项物料ID
                FROM
                    生产用料清单
                WHERE 组织ID = '100458'
                UNION ALL
                SELECT
                    产品ID,
                    工程ID,
                    子项物料ID
                FROM
                    委外用料清单
                WHERE 组织ID = '100458'
        ) A2 ON A2.产品ID = A1.ID
        RIGHT JOIN (
            SELECT
                ID,
                名称
            FROM 工程表
            WHERE 组织ID = '100458' AND 名称 = '{0}' AND 部门属性 = '基本生产部门'
        ) A3 ON A3.ID = A2.工程ID
        LEFT JOIN (
            SELECT
                物料ID,
                工程ID,
                下达日期
            FROM
                生产订单
            WHERE 组织ID = '100458'
            UNION ALL
            SELECT
                物料ID,
                工程ID,
                下达日期
            FROM
                委外订单
            WHERE 组织ID = '100458'
        ) A4 ON A4.工程ID = A3.ID
            AND A4.物料ID = A2.产品ID
        LEFT JOIN (
            SELECT
                物料ID,
                工程ID,
                允许采购数量,
                行业务关闭
            FROM
                采购申请单
            WHERE 组织ID = '100458'
        ) A5 ON A5.工程ID = A3.ID
            AND A5.物料ID = A2.子项物料ID
        LEFT JOIN (
            SELECT
                物料ID,
                工程ID,
                实收数量,
                审核日期
            FROM
                采购入库单
            WHERE 组织ID = '100458'
        ) A6 ON A6.工程ID = A3.ID
            AND A6.物料ID = A2.子项物料ID
        GROUP BY 产品名称
        ORDER BY 产品名称
        ";

        /// <summary>
        /// 转换成日期或NULL
        /// </summary>
        /// <param name="obj">转换对象</param>
        /// <returns>合法日期或NULL</returns>
        private DateTime? ToDateOrNull(object obj)
        {
            if (DateTime.TryParse($"{obj}", out DateTime date)) return date;
            else return null;
        }

        /// <summary>
        /// 解析EXCEL文件，生成采购进度和配置
        /// </summary>
        /// <param name="conf">配置</param>
        /// <returns>采购进度和配置</returns>
        public List<J数据中心_新能_采购进度> LoadFile(out J数据中心_新能_采购进度配置 conf)
        {
            List<object[]> f;
            using (var reader = ExcelReaderFactory.CreateReader(
                HttpContext.Current.Request.Files["file"].InputStream))
                f = reader.AsDataSet().Tables[0].Select().Select(x => x.ItemArray).ToList();

            string title = $"{f.ElementAt(0)[0]}";
            conf = new J数据中心_新能_采购进度配置
            {
                配置名称 = title,
                标题 = title,
                工程号 = regPNo.Match(title).Value
            };

            var fcolMap = new Dictionary<string, int>();
            var fcol = f.ElementAt(1);
            Match match;
            for (int i = 0; i < fcol.Length; i++)
            {
                match = regCol.Match($"{fcol[i]}");
                if (match.Success) fcolMap[match.Value] = i;
                else fcolMap[$"{fcol[i]}"] = i;
            }

            string curType = null;
            var progs = f.Skip(2).Select(
                x => new J数据中心_新能_采购进度
                {
                    类别 = $"{x[fcolMap["类别"]]}",
                    名称 = $"{x[fcolMap["类别"] + 1]}",
                    图纸下发计划日期 = ToDateOrNull(x[fcolMap["图纸"]]),
                    材料到货计划日期 = ToDateOrNull(x[fcolMap["材料"]])
                }
            ).ToList();
            progs.ForEach(x => {
                if (x.类别 != "") curType = x.类别;
                else x.类别 = curType;
            });

            return progs.Take(progs.IndexOf(progs.First(x => x.类别 == "打包"))).ToList();
        }

        /// <summary>
        /// 上传并解析文件
        /// </summary>
        /// <returns>解析结果</returns>
        [HttpPost]
        public IHttpActionResult Upload()
        {
            IEnumerable<BOM> bom, bomMatch;
            var progs = LoadFile(out var conf);
            string sqlbom = string.Format(sqlBom, conf.工程号);
            string sqlcus = string.Format(sqlCus, conf.工程号);
            using (var db = new SqlConnection(CONNSTR_IMG))
            {
                bom = db.Query<BOM>(sqlbom);
                conf.公司名称 = db.QueryFirst(sqlcus)?.名称;
            }

            string regMat,type,name;
            var mat = new List<dynamic>();
            progs.ForEach(x => {
                x.类别 = regBnk.Replace($"{x.类别}", "");
                x.名称 = regBnk.Replace($"{x.名称}", "");
                type = regSep.Replace(x.类别, "|").Trim('|');
                name = regSep.Replace(x.名称, "|").Trim('|');
                regMat = $@".*\.({type}).*({name})$";
                bomMatch = bom.Where(b => Regex.IsMatch(b.产品名称, regMat));

                if (bomMatch.Count() > 0)
                {
                    mat.Add(new
                    {
                        x.类别,
                        x.名称,
                        x.图纸下发计划日期,
                        x.材料到货计划日期,
                        产品名称 = bomMatch.Select(b => b.产品名称).ToArray(),
                        图纸下发实际日期 = (bomMatch.Where(b => b.图纸下发实际日期 == null).Count() == 0) ? 
                                        bomMatch.Max(b => b.图纸下发实际日期) : null,
                        材料到货实际日期 = (bomMatch.Where(b => b.材料到货实际日期 == null).Count() == 0) ?
                                        bomMatch.Max(b => b.材料到货实际日期) : null,
                        订单完成数量 = bomMatch.Sum(b => b.订单完成数量),
                        到货完成数量 = bomMatch.Sum(b => b.到货完成数量),
                        单据数量 = bomMatch.Sum(b => b.单据数量)
                    });
                }
                else mat.Add(new 
                    { 
                        x.类别, 
                        x.名称, 
                        x.图纸下发计划日期, 
                        x.材料到货计划日期,
                        产品名称 = new string[0]
                    });
            });

            return Json(new { conf, mat, bom });
        }
        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Load([FromBody] J数据中心_新能_采购进度配置 conf)
        {
            string sqlmat = $"SELECT * FROM J数据中心_新能_采购进度 WHERE 配置ID = {conf.ID}";
            string sqlbom = string.Format(sqlBom, conf.工程号);

            using (var dbIMG = new SqlConnection(CONNSTR_IMG))
            {
                var bom = dbIMG.Query<BOM>(sqlbom);
                using (var dbCE = new SqlConnection(CONNSTR_CE))
                {
                    var mat = from A1 in dbCE.Query<J数据中心_新能_采购进度>(sqlmat)
                              join A2 in bom on A1.产品名称 equals A2.产品名称 into J
                              from A3 in J.DefaultIfEmpty()
                              select new
                              {
                                  A1.类别,
                                  A1.名称,
                                  A1.图纸下发计划日期,
                                  A1.材料到货计划日期,
                                  A1.产品名称,
                                  A3?.图纸下发实际日期,
                                  A3?.材料到货实际日期,
                                  订单完成数量 = (A3 == null) ? 0 : A3.订单完成数量,
                                  到货完成数量 = (A3 == null) ? 0 : A3.到货完成数量,
                                  单据数量 = (A3 == null) ? 0 : A3.单据数量
                              } into A4
                              group A4 by new
                              {
                                  A4.类别,
                                  A4.名称,
                                  A4.图纸下发计划日期,
                                  A4.材料到货计划日期
                              } into G
                              select new
                              {
                                  G.Key.类别,
                                  G.Key.名称,
                                  G.Key.图纸下发计划日期,
                                  G.Key.材料到货计划日期,
                                  产品名称 = G.Where(b => !string.IsNullOrEmpty(b.产品名称)).Select(b => b.产品名称),
                                  图纸下发实际日期 = (G.Where(b => b.图纸下发实际日期 == null).Count() == 0) ?
                                        G.Max(b => b.图纸下发实际日期) : null,
                                  材料到货实际日期 = (G.Where(b => b.材料到货实际日期 == null).Count() == 0) ?
                                        G.Max(b => b.材料到货实际日期) : null,
                                  订单完成数量 = G.Sum(b => b.订单完成数量),
                                  到货完成数量 = G.Sum(b => b.到货完成数量),
                                  单据数量 = G.Sum(b => b.单据数量)
                              };
                    return Json(new { conf, mat, bom });
                }
            }
        }

        [HttpGet]
        public IHttpActionResult GetMats(string pno)
        {
            string sql = $@"
                SELECT
                    物料名称 
                FROM 物料表 
                WHERE 物料名称 LIKE '%{pno}%'
                GROUP BY 物料名称";
            using (var db = new SqlConnection(CONNSTR_IMG))
                return Json(db.Query(sql));
        }

        /// <summary>
        /// 获取配置列表
        /// </summary>
        /// <returns>配置列表</returns>
        [HttpGet]
        public IHttpActionResult GetConfs()
        {
            string sql = "SELECT * FROM J数据中心_新能_采购进度配置 ORDER BY 修改日期 DESC";
            using (var db = new SqlConnection(CONNSTR_CE))
                return Json(db.Query<J数据中心_新能_采购进度配置>(sql));
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        /// <param name="conf">配置</param>
        /// <returns></returns>
        public IHttpActionResult DelConf(J数据中心_新能_采购进度配置 conf)
        {
            try
            {
                using (var db = new SqlConnection(CONNSTR_CE))
                {
                    db.Delete(conf);
                    db.Execute($"DELETE FROM J数据中心_新能_采购进度 WHERE 配置ID={conf.ID}");
                }
                return Ok();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="req">数据</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Save([FromBody] JObject req)
        {
            var conf = req["conf"].ToObject<J数据中心_新能_采购进度配置>();
            string sql = $"SELECT ID FROM J数据中心_新能_采购进度配置 WHERE 配置名称 = '{conf.配置名称}'";
            conf.修改日期 = DateTime.Now;
            
            var mat = new List<J数据中心_新能_采购进度>();
            req["mat"].Children().ToList().ForEach(v =>
            {
                var row = v.ToObject<J数据中心_新能_采购进度>();
                mat.Add(new J数据中心_新能_采购进度
                {
                    类别 = row.类别,
                    名称 = row.名称,
                    图纸下发计划日期 = row.图纸下发计划日期,
                    材料到货计划日期 = row.材料到货计划日期,
                    产品名称 = row.产品名称
                });
            });

            using (var db = new SqlConnection(CONNSTR_CE))
            {
                var dbConf = db.Query<J数据中心_新能_采购进度配置>(sql);
                if (dbConf.Count() > 0)
                {
                    var ids = string.Join(",", dbConf.Select(x => x.ID));
                    db.Execute($@"DELETE FROM J数据中心_新能_采购进度 
                        WHERE 配置ID IN({ids})");
                    db.Execute($@"DELETE FROM J数据中心_新能_采购进度配置 
                        WHERE ID IN({ids})");
                }
                db.Insert(conf);
                int ID = db.QueryFirst(sql).ID;
                mat.ForEach(v => v.配置ID = ID);
                db.Insert(mat);
            }
            return Ok();
        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="req">数据</param>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult Print([FromBody]JObject req)
        {
            if (req != null && req.ToString() != "" && req.ToString() != "{}")
            {
                session["sel"] = req;
                return Ok();
            }
            else
            {
                var res = session["sel"];
                session.Remove("sel");
                return Json(res);
            }
        }
    }
}
