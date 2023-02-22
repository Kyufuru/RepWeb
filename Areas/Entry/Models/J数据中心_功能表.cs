using Dapper.Contrib.Extensions;

namespace 报表综合平台.Areas.Entry.Models
{
    /// <summary>
    ///     
    /// </summary>
    [Table("J数据中心_功能表")]
    public class J数据中心_功能表
    {
        [Key]
        public int ID { get; set; }
        public string 报表类别 { get; set; }
        public string 报表名称 { get; set; }
        public string 链接 { get; set; }
    }
}