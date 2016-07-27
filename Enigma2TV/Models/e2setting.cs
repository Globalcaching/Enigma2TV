using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2setting")]
    public class e2setting
    {
        [XmlElement("e2settingname")]
        public string e2settingname { get; set; }

        [XmlElement("e2settingvalue")]
        public string e2settingvalue { get; set; }
    }
}
