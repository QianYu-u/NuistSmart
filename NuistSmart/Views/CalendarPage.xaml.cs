using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.ViewModels;
using System;

namespace NuistSmart.Views
{
    public sealed partial class CalendarPage : Page
    {
        public CalendarViewModel ViewModel { get; }

        public CalendarPage()
        {
            this.InitializeComponent();
            ViewModel = App.ServiceProvider.GetRequiredService<CalendarViewModel>();
            
            this.Loaded += CalendarPage_Loaded;
        }

        private void CalendarPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Only load initially if we don't have data
            if (ViewModel.Semester1Weeks.Count == 0 && ViewModel.Semester2Weeks.Count == 0)
            {
                ViewModel.LoadInitialCommand.Execute(null);
            }
        }
    }
}
