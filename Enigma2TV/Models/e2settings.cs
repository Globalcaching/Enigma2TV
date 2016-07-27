using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlRoot("e2settings")]
    public class e2settingslist
    {
        [XmlElement("e2setting")]
        public List<e2setting> e2settings { get; set; }
    }
}
