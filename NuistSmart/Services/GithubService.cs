using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NuistSmart.Services
{
    /// <summary>
    /// GitHub 服务接口，用于获取在线公告等信息
    /// </summary>
    public interface IGithubService
    {
        Task<string> GetAdvisoryAsync();
    }

    /// <summary>
    /// GitHub 服务实现
    /// </summary>
    public class GithubService : IGithubService
    {
        private readonly HttpClient _httpClient;
        private const string AdvisoryUrl = "https://raw.githubusercontent.com/QianYu-u/NuistSmart/main/advisory.md";
        
        // 默认欢迎语（当网络请求失败时使用）
        private const string DefaultWelcome = 
            "?? 欢迎使用南信大智能体！\n\n" +
            "这是一个专为南京信息工程大学师生打造的智能校园服务平台。\n\n" +
            "?? 主要功能：\n" +
            "? 课程查询与管理\n" +
            "? 成绩实时查看\n" +
            "? 校园资讯推送\n" +
            "? 便捷生活服务\n\n" +
            "?? 提示：当前处于离线模式，无法获取最新公告。";

        public GithubService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // 5秒超时，避免长时间等待
            };
        }

        public GithubService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 从 GitHub 获取公告内容（advisory.md）
        /// 如果请求失败（断网、404、超时等），静默失败并返回默认欢迎语
        /// </summary>
        public async Task<string> GetAdvisoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(AdvisoryUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // 如果获取到的内容为空，返回默认欢迎语
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return DefaultWelcome;
                    }
                    
                    return content;
                }
                else
                {
                    // HTTP 状态码不是成功（如 404、500 等），返回默认欢迎语
                    return DefaultWelcome;
                }
            }
            catch (HttpRequestException)
            {
                // 网络请求失败（断网、DNS 解析失败等）
                return DefaultWelcome;
            }
            catch (TaskCanceledException)
            {
                // 超时
                return DefaultWelcome;
            }
            catch (Exception)
            {
                // 其他未预料的异常，静默失败
                return DefaultWelcome;
            }
        }
    }
}
