using LiteDB;

namespace NuistSmart.Models
{
    /// <summary>
    /// 通讯录黄页缓存模型
    /// </summary>
    public class ContactCache
    {
        /// <summary>
        /// 唯一标识ID（随机生成或MD5）
        /// </summary>
        [BsonId]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 部门名称（左侧树节点名称）
        /// </summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// 姓名/职能/名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 办公地点
        /// </summary>
        public string WorkPlace { get; set; } = string.Empty;
    }
}
