using Dapper.Contrib.Extensions;
using System;

namespace 报表综合平台.Areas.Entry.Models
{
    /// <summary>
    /// 统计表
    /// </summary>
    [Table("J数据中心_统计表")]
    public class J数据中心_统计表
    {
        [Key]
        public int ID { get; set; }
        public int 用户ID { get; set; }
        public int 报表ID { get; set; }
        public string 用户 { get; set; }
        public string IP { get; set; }
        public string 报表名称 { get; set; }
        public DateTime? 服务器时间 { get; set; }
        public DateTime? 本地时间 { get; set; }
        public string 渠道 { get; set; }
    }
}