using Dapper.Contrib.Extensions;

namespace 报表综合平台.Areas.Entry.Models
{
    /// <summary>
    /// 用户表
    /// </summary>
    [Table("J数据中心_用户表")]
    public class J数据中心_用户表
    {
        [Key]
        public int ID { get; set; }
        public string 用户 { get; set; }
        public string 部门 { get; set; }
    }
}