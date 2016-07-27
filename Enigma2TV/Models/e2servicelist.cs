using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlRoot("e2servicelist")]
    public class e2servicelist
    {
        [XmlElement("e2service")]
        public List<e2service> e2services { get; set; } 
    }
}
