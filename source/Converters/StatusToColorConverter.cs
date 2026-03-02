using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemChecker.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new SolidColorBrush(Colors.Gray);
            }

            string status = value.ToString();

            // Check symbol typically indicates success
            if (status.Contains("✓") || status.Contains("✔"))
            {
                return new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
            }
            // Cross symbol indicates failure
            else if (status.Contains("✗") || status.Contains("✘"))
            {
                return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            }
            // Warning or partial match
            else if (status.Contains("⚠") || status.Contains("~"))
            {
                return new SolidColorBrush(Color.FromRgb(251, 146, 60)); // Orange
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}