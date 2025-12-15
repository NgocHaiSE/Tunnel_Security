using Microsoft.UI.Xaml.Data;
using System;

namespace Station.Converters
{
    public class IndexToPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
      {
            if (value is int index)
    {
   // Each bar is 28px wide (18px bar + 10px spacing)
 return index * 28;
          }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
    }
    }
}
