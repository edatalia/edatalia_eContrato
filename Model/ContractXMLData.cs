﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
   

    public class ContractXMLData
    {
        public int ContractID { get; set; }
        public Client ClientRequester { get; set; }

        public Guid ContractGuid { get; set; }
        public string TipoContrato { get; set; }
        public DateTime StartDate { get; set; }
        public double Import { get; set; }



    }
}