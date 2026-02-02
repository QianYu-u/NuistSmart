namespace NuistSmart.Models
{
    /// <summary>
    /// 用户模型 - 表示一个登录的用户信息
    /// </summary>
    public class User
    {
        /// <summary>
        /// 学号
        /// </summary>
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 可选：头像 URL
        /// </summary>
        public string? AvatarUrl { get; set; }

        public User() { }

        public User(string studentId, string name)
        {
            StudentId = studentId;
            Name = name;
        }
    }
}
