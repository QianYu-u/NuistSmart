using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuistSmart.Models;
using NuistSmart.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NuistSmart.ViewModels
{
    /// <summary>标记当前处于学期内还是学期间</summary>
    public enum SemesterType { First, Second, BetweenSemesters }

    /// <summary>假期信息摘要</summary>
    public record HolidayInfo(
        string Name,           // 假期名称
        DateTime StartDate,    // 开始日期
        DateTime EndDate,      // 结束日期
        bool IsOngoing         // 今天是否正在放假
    )
    {
        public int Duration => (int)(EndDate - StartDate).TotalDays + 1;
        public int DaysUntil => IsOngoing ? 0 : (int)(StartDate - DateTime.Today).TotalDays;
        public int DaysRemaining => IsOngoing ? (int)(EndDate - DateTime.Today).TotalDays + 1 : 0;
    }

    public partial class CalendarViewModel : ObservableObject
    {
        private readonly CalendarService _calendarService;
        private readonly DbService _dbService;

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private string headerText = "校历";
        [ObservableProperty] private string schoolYearText = "";
        [ObservableProperty] private string todayInfoText = "";
        [ObservableProperty] private string currentSchoolYearStr = "";

        // ── 放假倒计时卡片相关 ──
        [ObservableProperty] private bool hasHolidayInfo;
        [ObservableProperty] private string holidayBannerTitle = "";
        [ObservableProperty] private string holidayBannerSubtitle = "";
        [ObservableProperty] private bool isOnHoliday;        // 当前正在放假

        // ── 学期展开控制 ──
        [ObservableProperty] private bool isSemester1Expanded = true;
        [ObservableProperty] private bool isSemester2Expanded = false;
        [ObservableProperty] private bool isSemester1Current;  // 当前学期徽章
        [ObservableProperty] private bool isSemester2Current;

        public ObservableCollection<CalendarWeekRowViewModel> Semester1Weeks { get; } = new();
        public ObservableCollection<CalendarWeekRowViewModel> Semester2Weeks { get; } = new();

        public CalendarViewModel(CalendarService calendarService, DbService dbService)
        {
            _calendarService = calendarService;
            _dbService = dbService;
        }

        // ── Commands ──
        [RelayCommand] private async Task LoadInitialAsync() => await LoadCalendarDataAsync(null);
        [RelayCommand] private async Task RefreshAsync() => await LoadCalendarDataAsync(CurrentSchoolYearStr, forceRefresh: true);

        [RelayCommand]
        private async Task PreviousYearAsync()
        {
            if (int.TryParse(CurrentSchoolYearStr, out int year))
                await LoadCalendarDataAsync((year - 1).ToString());
        }

        [RelayCommand]
        private async Task NextYearAsync()
        {
            if (int.TryParse(CurrentSchoolYearStr, out int year))
                await LoadCalendarDataAsync((year + 1).ToString());
        }

        [RelayCommand] private void ToggleSemester1() => IsSemester1Expanded = !IsSemester1Expanded;
        [RelayCommand] private void ToggleSemester2() => IsSemester2Expanded = !IsSemester2Expanded;

        // ── 数据加载 ──
        private async Task LoadCalendarDataAsync(string? xn, bool forceRefresh = false)
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                CalendarApiResponse? data = null;

                if (!string.IsNullOrEmpty(xn) && !forceRefresh)
                {
                    data = _dbService.GetCalendarData(xn);
                    if (data != null && (DateTime.Now - data.LastUpdated).TotalDays > 7)
                        data = null;
                }

                if (data == null)
                {
                    int? yearToLoad = null;
                    if (int.TryParse(xn, out int yearVal)) yearToLoad = yearVal;
                    data = await _calendarService.FetchCalendarAsync(yearToLoad);
                    if (data != null) _dbService.SaveCalendarData(data);
                }

                if (data != null) UpdateUI(data);
                else HeaderText = "加载校历失败";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CalendarViewModel] LoadCalendarDataAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── UI 更新总入口 ──
        private void UpdateUI(CalendarApiResponse data)
        {
            if (data.SchoolYearData == null) return;

            CurrentSchoolYearStr = data.SchoolYearData.XN;
            if (int.TryParse(CurrentSchoolYearStr, out int xnInt))
                SchoolYearText = $"{xnInt}-{xnInt + 1}学年";

            if (data.CurrentDateData != null)
                TodayInfoText = $"今天: {data.CurrentDateData.GL} {data.CurrentDateData.XQ} 第{data.CurrentDateData.ZC}周 · 农历{data.CurrentDateData.NL}";

            string todayCode = data.CurrentDateData?.Today ?? DateTime.Now.ToString("yyyyMMdd");
            var events = data.SchoolYearData.SchoolCalendarEvent ?? new Dictionary<string, string>();

            // 收集所有日期码供学期检测使用
            var sem1Codes = CollectDateCodes(data.SchoolYearData.FirstMap);
            var sem2Codes = CollectDateCodes(data.SchoolYearData.SecondMap);

            // 检测当前学期，设置展开状态
            var activeSemester = DetectCurrentSemester(todayCode, sem1Codes, sem2Codes);
            IsSemester1Current = activeSemester == SemesterType.First;
            IsSemester2Current = activeSemester == SemesterType.Second;
            // 默认展开当前学期，另一个折叠（但切换学年后两个都展开）
            if (CurrentSchoolYearStr == data.SchoolYearData.XN)
            {
                IsSemester1Expanded = activeSemester == SemesterType.First || activeSemester == SemesterType.BetweenSemesters;
                IsSemester2Expanded = activeSemester == SemesterType.Second || activeSemester == SemesterType.BetweenSemesters;
                // 如果都在学期间，则两者都展开
                if (activeSemester == SemesterType.BetweenSemesters)
                {
                    IsSemester1Expanded = true;
                    IsSemester2Expanded = true;
                }
                else
                {
                    IsSemester1Expanded = activeSemester == SemesterType.First;
                    IsSemester2Expanded = activeSemester == SemesterType.Second;
                }
            }

            // 假期倒计时
            var holiday = FindNextHoliday(todayCode, events);
            if (holiday != null)
            {
                HasHolidayInfo = true;
                IsOnHoliday = holiday.IsOngoing;
                if (holiday.IsOngoing)
                {
                    HolidayBannerTitle = $"🎉 正在放假中 · {holiday.Name}";
                    HolidayBannerSubtitle = $"假期共 {holiday.Duration} 天 · 还剩 {holiday.DaysRemaining} 天结束（{holiday.EndDate:MM月dd日}）";
                }
                else
                {
                    HolidayBannerTitle = $"📅 距离{holiday.Name}还有 {holiday.DaysUntil} 天";
                    HolidayBannerSubtitle = $"{holiday.StartDate:MM月dd日} 开始 · 共 {holiday.Duration} 天假期";
                }
            }
            else
            {
                HasHolidayInfo = false;
            }

            // 构建周行数据
            Semester1Weeks.Clear();
            if (data.SchoolYearData.FirstMap != null)
                foreach (var kvp in data.SchoolYearData.FirstMap.OrderBy(k => int.TryParse(k.Key, out int v) ? v : 999))
                    Semester1Weeks.Add(BuildRow(kvp.Value, todayCode, events));

            Semester2Weeks.Clear();
            if (data.SchoolYearData.SecondMap != null)
                foreach (var kvp in data.SchoolYearData.SecondMap.OrderBy(k => int.TryParse(k.Key, out int v) ? v : 999))
                    Semester2Weeks.Add(BuildRow(kvp.Value, todayCode, events));
        }

        // ── 辅助：将 Map 中所有日期码提取为有序集合 ──
        private static SortedSet<string> CollectDateCodes(Dictionary<string, CalendarWeekDto>? map)
        {
            var set = new SortedSet<string>(StringComparer.Ordinal);
            if (map == null) return set;
            foreach (var week in map.Values)
            {
                foreach (var day in new[] { week.Mon, week.Tue, week.Wed, week.Thu, week.Fri, week.Sat, week.Sun })
                    if (day != null && !string.IsNullOrEmpty(day.Code))
                        set.Add(day.Code);
            }
            return set;
        }

        // ── 检测当前处于哪个学期 ──
        private static SemesterType DetectCurrentSemester(string todayCode, SortedSet<string> sem1, SortedSet<string> sem2)
        {
            if (sem1.Contains(todayCode)) return SemesterType.First;
            if (sem2.Contains(todayCode)) return SemesterType.Second;
            // 不在任何学期的上课日（假期/周末区间）— 判断离哪个学期更近
            // 使用今天是否在两个学期之间来判断
            string sem1Start = sem1.Min ?? "";
            string sem1End   = sem1.Max ?? "";
            string sem2Start = sem2.Min ?? "";
            // 学期一结束到学期二开始之间 → BetweenSemesters
            // 简化：只要不在任一学期的日期集合内，就算 Between
            return SemesterType.BetweenSemesters;
        }

        // ── 查找下一个/当前假期 ──
        private static HolidayInfo? FindNextHoliday(string todayCode, Dictionary<string, string> events)
        {
            if (events == null || events.Count == 0) return null;

            // 解析事件字典 => (date, name) 并按日期排序
            var parsedEvents = new List<(DateTime Date, string Name)>();
            foreach (var kv in events)
            {
                if (DateTime.TryParseExact(kv.Key, "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
                {
                    parsedEvents.Add((d, kv.Value));
                }
            }
            parsedEvents.Sort((a, b) => a.Date.CompareTo(b.Date));

            var today = DateTime.Today;

            // 聚合连续同名假期为 segment
            var segments = new List<(string Name, DateTime Start, DateTime End)>();
            int i = 0;
            while (i < parsedEvents.Count)
            {
                var (startDate, name) = parsedEvents[i];
                var end = startDate;
                int j = i + 1;
                // 向后合并：同名且日期差不超过3天（含调休补丁）
                while (j < parsedEvents.Count &&
                       parsedEvents[j].Name == name &&
                       (parsedEvents[j].Date - end).TotalDays <= 3)
                {
                    end = parsedEvents[j].Date;
                    j++;
                }
                segments.Add((name, startDate, end));
                i = j;
            }

            // 找今天在假期中 or 今天之后最近的假期
            foreach (var seg in segments)
            {
                if (today >= seg.Start && today <= seg.End)
                    return new HolidayInfo(seg.Name, seg.Start, seg.End, IsOngoing: true);
            }
            foreach (var seg in segments)
            {
                if (seg.Start > today)
                    return new HolidayInfo(seg.Name, seg.Start, seg.End, IsOngoing: false);
            }
            return null;
        }

        // ── 构建周行 ──
        private CalendarWeekRowViewModel BuildRow(CalendarWeekDto weekDto, string todayCode, Dictionary<string, string> events)
        {
            var row = new CalendarWeekRowViewModel();
            if (weekDto.Month != null)
                row.MonthName = weekDto.Month.Name;

            if (weekDto.ZC != null && weekDto.ZC.Name.HasValue)
            {
                if (weekDto.ZC.Name.Value.ValueKind == JsonValueKind.Number)
                    row.WeekName = weekDto.ZC.Name.Value.GetInt32().ToString();
                else
                    row.WeekName = weekDto.ZC.Name.Value.GetString() ?? "";
                row.WeekSup = weekDto.ZC.ZcSup;
            }

            row.Mon = CreateDay(weekDto.Mon, todayCode, events);
            row.Tue = CreateDay(weekDto.Tue, todayCode, events);
            row.Wed = CreateDay(weekDto.Wed, todayCode, events);
            row.Thu = CreateDay(weekDto.Thu, todayCode, events);
            row.Fri = CreateDay(weekDto.Fri, todayCode, events);
            row.Sat = CreateDay(weekDto.Sat, todayCode, events);
            row.Sun = CreateDay(weekDto.Sun, todayCode, events);

            return row;
        }

        private CalendarDayViewModel CreateDay(CalendarDayDto? d, string todayCode, Dictionary<string, string> events)
        {
            if (d == null || string.IsNullOrEmpty(d.Name))
                return new CalendarDayViewModel { IsEmpty = true };

            string eventName = "";
            if (!string.IsNullOrEmpty(d.Code) && events.TryGetValue(d.Code, out string ev))
                eventName = ev;

            return new CalendarDayViewModel
            {
                DateNumber = d.Name,
                DateCode = d.Code,
                LunarText = !string.IsNullOrEmpty(eventName) ? eventName : d.LunarCalendar,
                NameColorHex = ExtractColor(d.NameColor),
                BgColorHex = ExtractBgColor(d.Background),
                LunarColorHex = ExtractColor(d.LunarCalendarColor),
                IsToday = d.Code == todayCode
            };
        }

        private static string ExtractColor(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            if (code.StartsWith("#")) return code;
            int index = code.IndexOf('#');
            return index >= 0 ? code.Substring(index, 7) : "";
        }

        private static string ExtractBgColor(string background)
        {
            if (string.IsNullOrEmpty(background)) return "";
            int index = background.IndexOf('#');
            return index >= 0 ? background.Substring(index, 7) : "";
        }
    }

    // ── 子 ViewModel ──
    public class CalendarWeekRowViewModel
    {
        public string MonthName { get; set; } = "";
        public string WeekName { get; set; } = "";
        public string WeekSup { get; set; } = "";
        public CalendarDayViewModel? Mon { get; set; }
        public CalendarDayViewModel? Tue { get; set; }
        public CalendarDayViewModel? Wed { get; set; }
        public CalendarDayViewModel? Thu { get; set; }
        public CalendarDayViewModel? Fri { get; set; }
        public CalendarDayViewModel? Sat { get; set; }
        public CalendarDayViewModel? Sun { get; set; }
    }

    public class CalendarDayViewModel
    {
        public bool IsEmpty { get; set; }
        public string DateNumber { get; set; } = "";
        public string LunarText { get; set; } = "";
        public string DateCode { get; set; } = "";
        public string NameColorHex { get; set; } = "";
        public string LunarColorHex { get; set; } = "";
        public string BgColorHex { get; set; } = "";
        public bool IsToday { get; set; }
        public bool HasEvent => LunarText != "";
    }
}
