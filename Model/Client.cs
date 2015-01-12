using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edatalia_signplyRT.Model
{
    public class Client
    {
        public int ClientID
        { get; set; }
        public string DNI { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public DateTime? BornDate { get; set; }
        public SexEnum? Sex { get; set; }
        public string Telephone { get; set; }
        public string Mail { get; set; }
        public string Address { get; set; }
        public int? PostalCode { get; set; }
        public string City { get; set; }
    }

    public enum SexEnum { Male, Female };

  }
