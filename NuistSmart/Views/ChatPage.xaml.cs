using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;

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
    }
}
