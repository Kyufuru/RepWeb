using Dapper.Contrib.Extensions;
using System;

namespace 报表综合平台.Areas.Report.Models
{
    [Table("J数据中心_新能_采购进度配置")]
    public class J数据中心_新能_采购进度配置
    {
        [Key]
        public int ID { get; set; }
        public string 配置名称 { get; set; }
        public string 标题 { get; set; }
        public string 工程号 { get; set; }
        public string 公司名称 { get; set; }
        public string 负责人 { get; set; }
        public DateTime? 项目开始日期 { get; set; }
        public int 调整滚动日期 { get; set; }
        public DateTime 修改日期 { get; set; }
    }
}