using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2event")]
    public class e2event
    {
        [XmlElement("e2eventservicereference")]
        public string e2eventservicereference { get; set; }

        [XmlElement("e2eventservicename")]
        public string e2eventservicename { get; set; }

        [XmlElement("e2eventprovidername")]
        public string e2eventprovidername { get; set; }

        [XmlElement("e2eventid")]
        public string e2eventid { get; set; }

        [XmlElement("e2eventname")]
        public string e2eventname { get; set; }

        [XmlElement("e2eventtitle")]
        public string e2eventtitle { get; set; }

        [XmlElement("e2eventdescription")]
        public string e2eventdescription { get; set; }

        [XmlElement("e2eventstart")]
        public string e2eventstart { get; set; }

        [XmlElement("e2eventduration")]
        public string e2eventduration { get; set; }

        [XmlElement("e2eventremaining")]
        public string e2eventremaining { get; set; }

        [XmlElement("e2eventcurrenttime")]
        public string e2eventcurrenttime { get; set; }

        [XmlElement("e2eventdescriptionextended")]
        public string e2eventdescriptionextended { get; set; }
    }
}
