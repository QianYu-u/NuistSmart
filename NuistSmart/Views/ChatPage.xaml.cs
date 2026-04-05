using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;
using NuistSmart.Models;

namespace NuistSmart.Views
{
    public sealed partial class ChatPage : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatPage()
        {
            this.InitializeComponent();

            // 从 DI 容器获取 ViewModel
            ViewModel = App.ServiceProvider.GetRequiredService<ChatViewModel>();
        }

        /// <summary>
        /// 会话列表点击事件
        /// </summary>
        private void SessionList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ChatSession session)
            {
                ViewModel.SwitchSessionCommand.Execute(session);
            }
        }
    }
}
