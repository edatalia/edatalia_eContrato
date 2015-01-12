using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT
{
    [DataContract]
    public class WidgetSize
    {
        [DataMember]
        public int Width;
        [DataMember]
        public int Height;
    }

    [DataContract]
    public class WidgetPosition
    {
        [DataMember]
        public int X;
        [DataMember]
        public int Y;
    }
}
