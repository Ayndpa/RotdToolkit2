using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RotdToolkit2
{
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Compares two bound values; returns the accent brush for the larger one so the
    /// newer / higher side of a comparison stands out, and the normal text brush otherwise.
    /// </summary>
    public sealed class HighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var normal = Application.Current?.TryFindResource("PrimaryTextBrush") as Brush ?? Brushes.White;
            var accent = Application.Current?.TryFindResource("PrimaryBrush") as Brush ?? Brushes.DodgerBlue;

            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
            {
                return normal;
            }

            if (values[0] is IComparable comparable)
            {
                try
                {
                    if (comparable.CompareTo(values[1]) > 0)
                    {
                        return accent;
                    }
                }
                catch
                {
                    // mismatched types: fall through to normal
                }
            }

            return normal;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Maps a save state string to a semantic brush: green when present, red on error, grey otherwise.
    /// </summary>
    public sealed class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string state = value as string ?? string.Empty;
            string key = state.Contains("已存在")
                ? "SuccessBrush"
                : state.Contains("失败") || state.Contains("错误")
                    ? "DangerBrush"
                    : "SecondaryBorderBrush";

            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

