using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlRoot("e2eventlist")]
    public class e2eventlist
    {
        [XmlElement("e2event")]
        public List<e2event> e2events { get; set; }
    }
}
