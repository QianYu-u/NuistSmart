using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace NuistSmart.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean) return !boolean;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean) return !boolean;
            return value;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && !b) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字符串非空时 Visible，否则 Collapsed
    /// </summary>
    public class StringToBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex) && hex.StartsWith("#"))
            {
                try
                {
                    hex = hex.Replace("#", string.Empty);
                    byte a = 255;
                    byte r = 255;
                    byte g = 255;
                    byte b = 255;

                    if (hex.Length == 8)
                    {
                        a = System.Convert.ToByte(hex.Substring(0, 2), 16);
                        r = System.Convert.ToByte(hex.Substring(2, 2), 16);
                        g = System.Convert.ToByte(hex.Substring(4, 2), 16);
                        b = System.Convert.ToByte(hex.Substring(6, 2), 16);
                    }
                    else if (hex.Length == 6)
                    {
                        r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                        g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                        b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    }

                    return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
                }
                catch { }
            }
            
            // Fallback
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字符串相等性检查转为Visibility
    /// </summary>
    public class StringEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && parameter is string target)
            {
                return str == target ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}