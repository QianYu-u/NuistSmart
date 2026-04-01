using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NuistSmart.Models;
using NuistSmart.Services;
using NuistSmart.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuistSmart.Views
{
    public sealed partial class CalendarPage : Page
    {
        private readonly ShellViewModel _viewModel;

        public CalendarPage()
        {
            this.InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            
            // 页面加载时自动刷新假期数据
            this.Loaded += CalendarPage_Loaded;
        }

        private async void CalendarPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHolidaysAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadHolidaysAsync();
        }

        private async System.Threading.Tasks.Task LoadHolidaysAsync()
        {
            try
            {
                // 显示加载状态
                LoadingRing.IsActive = true;
                LoadingRing.Visibility = Visibility.Visible;
                RefreshButton.IsEnabled = false;
                StatusText.Text = "正在获取假期数据...";

                // 更新校历数据
                await _viewModel.UpdateCalendarAsync();

                // 获取当前月份和下个月的假期
                var dbService = App.ServiceProvider.GetRequiredService<DbService>();
                var now = DateTime.Now;
                
                var currentMonthHolidays = dbService.GetHolidaysByMonth(now.Year, now.Month);
                var nextMonth = now.AddMonths(1);
                var nextMonthHolidays = dbService.GetHolidaysByMonth(nextMonth.Year, nextMonth.Month);
                
                // 合并并排序
                var allHolidays = currentMonthHolidays.Concat(nextMonthHolidays)
                    .OrderBy(h => h.Date)
                    .ToList();

                // 转换为显示用的 ViewModel
                var displayItems = allHolidays.Select(h => new HolidayDisplayItem
                {
                    Date = h.Date,
                    Name = h.Name,
                    IsLongVacation = h.IsLongVacation
                }).ToList();

                // 更新 UI
                HolidaysItemsControl.ItemsSource = displayItems;

                StatusText.Text = $"已加载 {displayItems.Count} 个假期";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"加载失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CalendarPage] 加载假期失败: {ex.Message}");
            }
            finally
            {
                // 隐藏加载状态
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
                RefreshButton.IsEnabled = true;
            }
        }
    }

    /// <summary>
    /// 用于显示的假期项
    /// </summary>
    public class HolidayDisplayItem
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsLongVacation { get; set; }

        public string DateText => Date.ToString("MM月dd日");
        public string DayOfWeekText => GetDayOfWeekText(Date.DayOfWeek);
        public Visibility LongVacationVisibility => IsLongVacation ? Visibility.Visible : Visibility.Collapsed;

        private string GetDayOfWeekText(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                DayOfWeek.Sunday => "周日",
                _ => ""
            };
        }
    }
}
