using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2simplexmlresult")]
    public class e2simplexmlresult
    {
        [XmlElement("e2state")]
        public string e2state { get; set; }

        [XmlElement("e2statetext")]
        public string e2statetext { get; set; }
    }
}
