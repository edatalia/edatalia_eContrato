using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
    public class Citizen
    {
        public int CitizenID
        { get; set; }
        public string DNI { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Nationality { get; set; }
        public string PassportNumber { get; set; }
        public LanguageEnum? ComLanguage { get; set; }
        public string Street { get; set; }

        public int? Number{get;set;}

        public string Bis {get;set;}

        public string Staircase {get;set;}

        public string Floor{get;set;}

        public string Hand {get;set;}

        public string Door {get;set;}

        public string Country { get; set; }

        public string City { get; set; }

        public int? PostalCode {get;set;}

        public string Telephone { get; set; }

        public string MobilePhone { get; set; }
        
        public string Mail { get; set; }
      
    }

    public enum LanguageEnum { Euskera, Castellano };
}
