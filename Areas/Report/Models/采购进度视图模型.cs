using System;

namespace 报表综合平台.Areas.Report.Models
{
    public class 采购进度视图模型
    {
        public string 类别 { get; set; }
        public string 名称 { get; set; }
        public DateTime? 图纸下发计划日期 { get; set; }
        public DateTime? 材料到货计划日期 { get; set; }
        public string[] 产品名称 { get; set; }
        public DateTime? 图纸下发实际日期 { get; set; }
        public DateTime? 材料到货实际日期 { get; set; }
        public float? 订单完成数量 { get; set; }
        public float? 到货完成数量 { get; set; }
        public float? 单据数量 { get; set; }
    }
}