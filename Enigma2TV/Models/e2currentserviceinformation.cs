using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2currentserviceinformation")]
    public class e2currentserviceinformation
    {
        [XmlElement("e2service")]
        public e2service e2service { get; set; }

        [XmlArray("e2eventlist")]
        [XmlArrayItem("e2event")]
        public List<e2event> e2events { get; set; }
    }
}
