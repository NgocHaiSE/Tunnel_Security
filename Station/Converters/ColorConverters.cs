using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Station.Converters
{
    public class BoolToAccentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;

            // Default keys based on app styles
            string trueKey = "PrimaryDarkBrush";
            string falseKey = "TextSecondaryBrush";

            if (parameter is string paramStr)
            {
                var parts = paramStr.Split('|');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) trueKey = parts[0];
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) falseKey = parts[1];
            }

            string targetKey = isTrue ? trueKey : falseKey;

            if (targetKey.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            }

            if (Application.Current.Resources.TryGetValue(targetKey, out object resource))
            {
                return resource;
            }

            // Fallback
            return isTrue
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) // Green-600
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 107, 114, 128)); // Gray-500
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToInverseColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;

            // Default: True -> White (Inverse), False -> TextPrimary (Normal)
            string trueKey = "WhiteBrush"; // Assuming WhiteBrush exists or we use a fallback
            string falseKey = "TextPrimaryBrush";

            if (parameter is string paramStr)
            {
                var parts = paramStr.Split('|');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) trueKey = parts[0];
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) falseKey = parts[1];
            }

            // Handle hardcoded colors for safety
            if (trueKey.Equals("White", StringComparison.OrdinalIgnoreCase) || trueKey.Equals("WhiteBrush", StringComparison.OrdinalIgnoreCase))
            {
                if (isTrue) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            }
            if (falseKey.Equals("White", StringComparison.OrdinalIgnoreCase) || falseKey.Equals("WhiteBrush", StringComparison.OrdinalIgnoreCase))
            {
                if (!isTrue) return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            }

            string targetKey = isTrue ? trueKey : falseKey;

            if (Application.Current.Resources.TryGetValue(targetKey, out object resource))
            {
                return resource;
            }

            // Fallback
            return isTrue
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 41, 59)); // Slate-800
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts bool to accent background brush (for tab highlighting)
    /// True: BackgroundAccentBrush, False: Transparent
    /// </summary>
    public class BoolToAccentBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;

            if (isTrue)
            {
                // Return accent background for selected state
                if (Application.Current.Resources.TryGetValue("BackgroundAccentBrush", out object resource))
                {
                    return resource;
                }
                // Fallback accent color
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 252, 231));
            }
            else
            {
                // Return transparent for unselected state
                return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
