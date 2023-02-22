using Dapper.Contrib.Extensions;
using System;

namespace 报表综合平台.Areas.Report.Models
{
    [Table("J数据中心_新能_采购进度")]
    public class J数据中心_新能_采购进度
    {
        [Key]
        public int ID { get; set; }
        public int 配置ID { get; set; }
        public string 类别 { get; set; }
        public string 名称 { get; set; }
        public string 产品名称 { get; set; }
        public DateTime? 图纸下发计划日期 { get; set; }
        public DateTime? 材料到货计划日期 { get; set; }
    }
}