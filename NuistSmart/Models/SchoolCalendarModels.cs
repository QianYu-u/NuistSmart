using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media;

namespace NuistSmart.Models
{
    public class CalendarApiResponse
    {
        // 供 LiteDB 主键使用
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("currentDateData")]
        public CurrentDateDto? CurrentDateData { get; set; }

        [JsonPropertyName("schoolYearData")]
        public SchoolYearDataDto? SchoolYearData { get; set; }
        
        // 用于控制缓存未过期逻辑
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class CurrentDateDto
    {
        [JsonPropertyName("TODAY")]
        public string Today { get; set; } = string.Empty;

        [JsonPropertyName("GL")]
        public string GL { get; set; } = string.Empty;

        [JsonPropertyName("NL")]
        public string NL { get; set; } = string.Empty;

        [JsonPropertyName("XQ")]
        public string XQ { get; set; } = string.Empty;

        [JsonPropertyName("XN")]
        public string XN { get; set; } = string.Empty;

        [JsonPropertyName("ZC")]
        public int ZC { get; set; }

        [JsonPropertyName("DAY_OF_MONTH")]
        public int DayOfMonth { get; set; }
    }

    public class SchoolYearDataDto
    {
        [JsonPropertyName("firstMap")]
        public Dictionary<string, CalendarWeekDto> FirstMap { get; set; } = new();

        [JsonPropertyName("secondMap")]
        public Dictionary<string, CalendarWeekDto> SecondMap { get; set; } = new();

        [JsonPropertyName("schoolCalendarEvent")]
        public Dictionary<string, string> SchoolCalendarEvent { get; set; } = new();

        [JsonPropertyName("XN")]
        public string XN { get; set; } = string.Empty;
    }

    public class CalendarWeekDto
    {
        [JsonPropertyName("MON")]
        public CalendarDayDto? Mon { get; set; }

        [JsonPropertyName("TUE")]
        public CalendarDayDto? Tue { get; set; }

        [JsonPropertyName("WED")]
        public CalendarDayDto? Wed { get; set; }

        [JsonPropertyName("THU")]
        public CalendarDayDto? Thu { get; set; }

        [JsonPropertyName("FRI")]
        public CalendarDayDto? Fri { get; set; }

        [JsonPropertyName("SAT")]
        public CalendarDayDto? Sat { get; set; }

        [JsonPropertyName("SUN")]
        public CalendarDayDto? Sun { get; set; }

        [JsonPropertyName("MONTH")]
        public CalendarMonthMetaDto? Month { get; set; }

        [JsonPropertyName("ZC")]
        public CalendarZcMetaDto? ZC { get; set; }
    }

    public class CalendarMonthMetaDto
    {
        [JsonPropertyName("NAME")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("CODE")]
        public string Code { get; set; } = string.Empty;
        
        [JsonPropertyName("NAME_COLOR")]
        public string NameColor { get; set; } = string.Empty;
    }

    public class CalendarZcMetaDto
    {
        [JsonPropertyName("NAME")]
        public JsonElement? Name { get; set; }
        
        [JsonPropertyName("ZC_SUP")]
        public string ZcSup { get; set; } = string.Empty;
    }

    public class CalendarDayDto
    {
        [JsonPropertyName("CODE")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("NAME")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("LUNAR_CALENDAR")]
        public string LunarCalendar { get; set; } = string.Empty;

        [JsonPropertyName("NAME_COLOR")]
        public string NameColor { get; set; } = string.Empty;

        [JsonPropertyName("BACKGROUND")]
        public string Background { get; set; } = string.Empty;

        [JsonPropertyName("LUNAR_CALENDAR_COLOR")]
        public string LunarCalendarColor { get; set; } = string.Empty;
    }
}
