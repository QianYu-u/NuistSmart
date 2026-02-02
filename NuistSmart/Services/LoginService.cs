using System;
using System.Threading.Tasks;
using NuistSmart.Models;

namespace NuistSmart.Services
{
    /// <summary>
    /// 登录服务接口 - 定义登录相关的操作
    /// 使用接口是为了方便后续测试和替换真实实现
    /// </summary>
    public interface ILoginService
    {
        /// <summary>
        /// 异步登录方法
        /// </summary>
        /// <param name="studentId">学号</param>
        /// <param name="password">密码</param>
        /// <returns>登录成功返回 User 对象，失败返回 null</returns>
        Task<User?> LoginAsync(string studentId, string password);
    }

    /// <summary>
    /// 模拟登录服务实现
    /// 在实际项目中，这里会调用真实的 API
    /// </summary>
    public class LoginService : ILoginService
    {
        /// <summary>
        /// 模拟异步登录
        /// 硬编码账号：admin / 123456
        /// </summary>
        public async Task<User?> LoginAsync(string studentId, string password)
        {
            // 模拟网络延迟 1 秒
            // 在真实项目中，这里会是 HttpClient 调用后端 API
            await Task.Delay(1000);

            // 简单的硬编码验证
            // 实际项目中应该调用后端 API 验证
            if (studentId == "admin" && password == "123456")
            {
                // 登录成功，返回用户信息
                return new User
                {
                    StudentId = studentId,
                    Name = "张三",
                    AvatarUrl = null
                };
            }

            // 登录失败，返回 null
            return null;
        }
    }
}
