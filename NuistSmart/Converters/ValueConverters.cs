using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace NuistSmart.Converters
{
    /// <summary>
    /// 反转布尔值转换器
    /// 用途：当 IsLoading 为 true 时，返回 false，用于禁用控件
    /// 例如：IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBooleanConverter}}"
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 如果输入是布尔值，返回其反转值
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // 双向绑定时的反向转换
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// 字符串转布尔值转换器
    /// 用途：当字符串不为空时返回 true，为空时返回 false
    /// 常用于根据错误消息是否存在来控制 InfoBar 的显示
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 如果字符串不为空，返回 true
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // 这个转换器不支持反向转换
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值转可见性转换器
    /// 用途：true -> Visible, false -> Collapsed
    /// 常用于根据布尔值控制控件的显示/隐藏
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// 反转布尔值到可见性转换器
    /// 用途：true -> Collapsed, false -> Visible
    /// 常用于加载状态相反的显示控制
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }
}
