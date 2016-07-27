using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2powerstate")]
    public class e2powerstate
    {
        [XmlElement("e2instandby")]
        public bool e2instandby { get; set; }
    }
}
