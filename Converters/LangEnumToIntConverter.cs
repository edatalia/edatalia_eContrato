using Edatalia_signplyRT.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Edatalia_signplyRT.Converters
{
    class LangEnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return -1;

            LanguageEnum val = (LanguageEnum)value;

            if (LanguageEnum.Euskera == val) return 0;
            if (LanguageEnum.Castellano == val) return 1;

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((int)value == 0) return LanguageEnum.Euskera;
            else return LanguageEnum.Castellano;
        }
    }
}
