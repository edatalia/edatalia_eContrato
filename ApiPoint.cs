using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Input.Inking;

namespace Edatalia_signplyRT
{
    [DataContract]
    public class ApiPointTime
    {
        [DataMember]
        public double XPos;
         [DataMember]
        public double YPos;
         [DataMember]
        public double Pressure;
         [DataMember]
        public double TiltX;
         [DataMember]
        public double TiltY;
         [DataMember]
        public double Twist;
         [DataMember]
        public ulong TimeSpan;

        public ApiPointTime(PointerPoint pt)
        {
            XPos = pt.Position.X;
            YPos = pt.Position.Y;
            Pressure = pt.Properties.Pressure;
            TiltX = pt.Properties.XTilt;
            TiltY = pt.Properties.YTilt;
            Twist = pt.Properties.Twist;
            TimeSpan = pt.Timestamp;
            

        }
    }

    public class ApiSegment
    {
        public List<ApiPointTime> lstPoints;

        public ApiSegment()
        {
            lstPoints = new List<ApiPointTime>();
        }

        public void AddPoint(ApiPointTime pt)
        {
            this.lstPoints.Add(pt);
        }
    }
}
