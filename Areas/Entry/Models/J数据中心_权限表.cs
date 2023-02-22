using Dapper.Contrib.Extensions;

namespace 报表综合平台.Areas.Entry.Models
{
    /// <summary>
    /// 权限表
    /// </summary>
    [Table("J数据中心_权限表")]
    public class J数据中心_权限表
    {
        [Key]
        public int ID { get; set; }
        public int 用户ID { get; set; }
        public int 报表ID { get; set; }
        public string 用户 { get; set; }
        public string 报表名称 { get; set; }
    }
}