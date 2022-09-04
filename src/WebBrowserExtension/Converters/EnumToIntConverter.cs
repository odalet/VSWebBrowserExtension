using System;
using System.Globalization;
using System.Windows.Data;

namespace WebBrowserExtension.Converters
{
    ////public sealed class EnumToIntConverter : IValueConverter
    ////{
    ////    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    ////    {
    ////        if (value?.GetType().IsEnum == true && targetType == typeof(int))
    ////            return (int)value;
    ////        return value;
    ////    }

    ////    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    ////    {
    ////        if (targetType.IsEnum && value is int)
    ////            return Enum.ToObject(targetType, value);
    ////        return value;
    ////    }
    ////}

    public sealed class GetEnumValuesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Type type ? Enum.GetValues(type) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
