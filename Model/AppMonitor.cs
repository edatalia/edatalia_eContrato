using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
    public class AppMonitor
    {
        public int AppMonitorID
        { get; set; }
        public Guid AppMonitorGuid { get; set; }

        public DateTime? InstallationDate { get; set; }
    }
}
