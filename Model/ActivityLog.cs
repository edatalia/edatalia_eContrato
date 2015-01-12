using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
    public class ActivityLog
    {
        public int ActivityLogID
        { get; set; }
        public Guid AppGuid { get; set; }

        public DateTime? ActivityLogDate { get; set; }

        public string ActivityLogDescription { get; set; }

        public string ActivityLogParams { get; set; }
    }
}
