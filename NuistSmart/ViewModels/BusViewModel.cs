using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Services;
using Microsoft.UI.Dispatching;

namespace NuistSmart.ViewModels
{
    public partial class BusViewModel : ObservableObject
    {
        private readonly TokenCaptureService _tokenCaptureService;
        private readonly BusService _busService;
        private readonly DispatcherQueue _dispatcherQueue;

        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private string _targetStart = "金牛湖尚学楼";

        [ObservableProperty]
        private string _targetEnd = "本部文德楼";

        [ObservableProperty]
        private string _targetDate = DateTime.Now.ToString("yyyy-MM-dd");

        [ObservableProperty]
        private string _targetSpecificTime = "";

        [ObservableProperty]
        private string _targetHourRange = "";

        [ObservableProperty]
        private string _userToken = "";

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _isNotProcessing = true;

        partial void OnIsProcessingChanged(bool value)
        {
            IsNotProcessing = !value;
        }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> StationList { get; } = new ObservableCollection<string>
        {
            "金牛湖尚学楼",
            "本部文德楼",
            "金牛湖校区",
            "盘城"
        };

        public BusViewModel(TokenCaptureService tokenCaptureService, BusService busService)
        {
            _tokenCaptureService = tokenCaptureService;
            _busService = busService;
            // 捕获当前的 UI 线程调度器
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        private void AddLog(string message)
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(() => Logs.Add(message));
            }
            else
            {
                Logs.Add(message);
            }
        }

        [RelayCommand]
        public void ClearLogs()
        {
            Logs.Clear();
            AddLog(">> 日志输出控制台已清空 <<");
        }

        [RelayCommand]
        public void StopProcess()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                AddLog(">> 发送强制停止信号，等待当前拦截代理与轮询任务被安全回收... <<");
            }
        }

        [RelayCommand]
        public async Task StartProcessAsync()
        {
            if (IsProcessing) return;

            IsProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                ClearLogs();
                string token = UserToken?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(token))
                {
                    AddLog("未提供 Token，开始启动代理环境尝试提取微信小程序 Token...");
                    token = await _tokenCaptureService.StartCaptureAsync(AddLog, _cancellationTokenSource.Token);

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        UserToken = token; // 将提取到的 Token 更新至界面上
                    }
                }
                else
                {
                    AddLog("检测到您已手动预设 Token，跳过系统代理抓包！直接进入抢票模式...");
                }

                if (!string.IsNullOrEmpty(token))
                {
                    AddLog("顺利拿到系统级别校验 Token，准备转入全自动抢票轮询阶段");
                    await _busService.StartPollingAsync(
                        token,
                        TargetStart,
                        TargetEnd,
                        TargetDate,
                        TargetSpecificTime,
                        TargetHourRange,
                        AddLog,
                        _cancellationTokenSource.Token
                    );
                }
                else
                {
                    AddLog("未能取得 Token，即将退出程序块！");
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("操作已被用户强行干预取消并终止！");
            }
            catch (Exception ex)
            {
                AddLog($"运行时发生未预知的致命异常: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                AddLog(">> 自动抢票任务流程生命周期结束 <<");
            }
        }
    }
}
