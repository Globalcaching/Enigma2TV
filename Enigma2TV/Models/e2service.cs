using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Enigma2TV.Models
{
    [XmlType("e2service")]
    public class e2service
    {
        [XmlElement("e2servicereference")]
        public string e2servicereference { get; set; }

        [XmlElement("e2servicename")]
        public string e2servicename { get; set; }

        [XmlElement("e2providername", IsNullable = true)]
        public string e2providername { get; set; }

        [XmlElement("e2videowidth", IsNullable = true)]
        public string e2videowidth { get; set; }

        [XmlElement("e2videoheight", IsNullable = true)]
        public string e2videoheight { get; set; }

        [XmlElement("e2servicevideosize", IsNullable = true)]
        public string e2servicevideosize { get; set; }

        [XmlElement("e2iswidescreen", IsNullable = true)]
        public string e2iswidescreen { get; set; }

        [XmlElement("e2apid", IsNullable = true)]
        public string e2apid { get; set; }

        [XmlElement("e2vpid", IsNullable = true)]
        public string e2vpid { get; set; }

        [XmlElement("e2pcrpid", IsNullable = true)]
        public string e2pcrpid { get; set; }

        [XmlElement("e2pmtpid", IsNullable = true)]
        public string e2pmtpid { get; set; }

        [XmlElement("e2txtpid", IsNullable = true)]
        public string e2txtpid { get; set; }

        [XmlElement("e2tsid", IsNullable = true)]
        public string e2tsid { get; set; }

        [XmlElement("e2onid", IsNullable = true)]
        public string e2onid { get; set; }

        [XmlElement("e2sid", IsNullable = true)]
        public string e2sid { get; set; }
    }
}
