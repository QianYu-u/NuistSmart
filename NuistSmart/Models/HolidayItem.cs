using System;

namespace NuistSmart.Models
{
    /// <summary>
    /// 校历假期项，表示单个假期日期
    /// </summary>
    public class HolidayItem
    {
        /// <summary>
        /// 主键，使用日期字符串格式 "yyyy-MM-dd" 防止重复存储
        /// </summary>
        public string Id => Date.ToString("yyyy-MM-dd");
        
        /// <summary>
        /// 假期日期
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// 假期名称，如 "寒假", "国庆节", "周末"
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 标记是否为全校性长假（寒假、暑假）
        /// </summary>
        public bool IsLongVacation { get; set; }
    }
}
