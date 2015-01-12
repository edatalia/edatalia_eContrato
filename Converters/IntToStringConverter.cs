using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Edatalia_signplyRT.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int val;
            if (value != null)
            {
                val = (int)value;
                return val.ToString();
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try { return System.Convert.ToInt32((string)value); }
            catch(Exception ex)
            {
                string msg = ex.ToString();
            }

            return 0;
        }
    }
}
