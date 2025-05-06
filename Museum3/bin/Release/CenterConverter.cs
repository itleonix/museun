using System;
using System.Globalization;
using System.Windows.Data;

namespace Museum3
{
    public class CenterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double parentSize && parameter is string paramStr && double.TryParse(paramStr, out double elementSize))
            {
                return (parentSize - elementSize) / 2.0; // Центрирование
            }
            return 0.0; // Если что-то пошло не так
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
