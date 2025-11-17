using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Station.Converters
{
    public class PercentageToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double percentage)
            {
         // Convert percentage to angle (0-360 degrees)
           return percentage * 360;
        }
   return 0;
     }

  public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
   throw new NotImplementedException();
    }
    }

    public class AlertSeverityTotalConverter : IValueConverter
    {
 public object Convert(object value, Type targetType, object parameter, string language)
        {
            // This will be used to calculate total alerts for pie chart center
            return value?.ToString() ?? "0";
        }

public object ConvertBack(object value, Type targetType, object parameter, string language)
     {
            throw new NotImplementedException();
        }
    }
}
