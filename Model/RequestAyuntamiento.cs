using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
    public class RequestAyuntamiento
    {
        public Citizen RequesterCitizen { get; set; }
        public Citizen AuthorizedCitizen { get; set; }
        public string Admin { get; set; }
        public string Department { get; set; }
        public string AdminUnit { get; set; }
        public string Subject { get; set; }
        public string Cause { get; set; }
        public string Oposition { get; set; }

        public string LicencePlate { get; set; }

        public string ExpedientNumber { get; set; }
        public string Notes { get; set; }

        public bool WritingAttached { get; set; }
        public bool DocumentAttached { get; set; }
        public bool BankAttached { get; set; }
    }
}
