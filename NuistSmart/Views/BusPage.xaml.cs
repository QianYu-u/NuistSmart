using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;
using System.Collections.Specialized;

namespace NuistSmart.Views
{
    public sealed partial class BusPage : Page
    {
        public BusViewModel ViewModel { get; }

        public BusPage()
        {
            this.InitializeComponent();
            ViewModel = App.ServiceProvider.GetRequiredService<BusViewModel>();

            // 自动滚动到最新的日志
            ViewModel.Logs.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LogScrollViewer.UpdateLayout();
                        LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
                    });
                }
            };
        }
    }
}
