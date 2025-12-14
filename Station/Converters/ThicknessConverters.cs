using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Station.Converters
{
    public class BoolToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isTrue = value is bool b && b;
            double thickness = 1;

            if (parameter is string paramStr && double.TryParse(paramStr, out double result))
            {
                thickness = result;
            }

            // If true (Selected) -> BorderThickness = 0,0,0,2 (Bottom border) or just 1 depending on design
            // The image showed a full border or bottom border? 
            // Looking at the user request "nav của alertpage và devicepage như hình"
            // The image 1 shows: "Home" tab has a green border around it (or maybe just bottom?).
            // Let's look at the image description or re-read. 
            // "thiết kế phần nav ... như hình ... dùng màu hiện tại"
            // The uploaded image 1 shows a tab "Home" with a green outline (border).
            // So if Selected (True) -> Thickness = 1 (or parameter).
            // If Not Selected (False) -> Thickness = 0.

            return isTrue ? new Thickness(thickness) : new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
