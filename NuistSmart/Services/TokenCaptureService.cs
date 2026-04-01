using System;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace NuistSmart.Services
{
    public class TokenCaptureService
    {
        private ProxyServer? _proxyServer;

        /// <summary>
        /// 开启系统代理并拦截抓取目标 API 的 auth Token
        /// </summary>
        public async Task<string> StartCaptureAsync(Action<string> logCallback, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>();

            // 如果被上层取消，这里会自动取消整个前置抓包 Task
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled());

            _proxyServer = new ProxyServer(false, false, false);
            // 必须先确保证书已经被生成，否则直接 Trust 会抛出为空的致命异常
            _proxyServer.CertificateManager.EnsureRootCertificate();
            // 启用拦截 HTTPS 的根证书信任机制（如果本地没有，会自动让系统弹出证书安装框）
            _proxyServer.CertificateManager.TrustRootCertificate(true);

            var explicitEndPoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 8000, true);
            _proxyServer.AddEndPoint(explicitEndPoint);

            async Task OnBeforeRequest(object sender, SessionEventArgs e)
            {
                if (e.HttpClient.Request.RequestUri.Host.Contains("api.nuist.899988.xyz"))
                {
                    var authHeader = e.HttpClient.Request.Headers.GetFirstHeader("auth");
                    if (authHeader != null && !string.IsNullOrWhiteSpace(authHeader.Value))
                    {
                        var token = authHeader.Value;
                        // 取到第一份符合条件的 Token 即可
                        tcs.TrySetResult(token);
                    }
                }
                await Task.CompletedTask;
            }

            _proxyServer.BeforeRequest += OnBeforeRequest;

            try
            {
                _proxyServer.Start();
                try
                {
                    // 自动将我们的 proxy 设置为系统全局代理
                    _proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
                    _proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
                    logCallback?.Invoke("系统代理接管成功，正在监听流量...");
                }
                catch (NotSupportedException)
                {
                    logCallback?.Invoke("【代理权限受限】WinUI 3 容器环境无法直接修改系统级代理。");
                    logCallback?.Invoke("系统已转为内置嗅探。若需自动抓包，请前往系统“网络和 Internet -> 代理”将手动代理打开，并填入地址 127.0.0.1 和 端口 8000。或者您可以直接关闭并手动填入 Token。");
                }

                logCallback?.Invoke("【操作提示】请打开微信小程序加载校车界面");

                // 会在这里阻塞，直到 OnBeforeRequest 里拿到 Token 或 cancellationToken 触发超时/取消
                var token = await tcs.Task;
                logCallback?.Invoke($"Token提取成功: {token.Substring(0, Math.Min(30, token.Length))}...");
                return token;
            }
            finally
            {
                // 无论以何种形式退出（成功取到、被取消、挂了异常），务必清理环境，防止断网
                _proxyServer.BeforeRequest -= OnBeforeRequest;
                _proxyServer.RestoreOriginalProxySettings();
                _proxyServer.Stop();
                _proxyServer.Dispose();
                _proxyServer = null;
                logCallback?.Invoke("系统代理已断开恢复原状，抓包结束。");
            }
        }
    }
}
