using Enigma2TV.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

namespace Enigma2TV.Enigma
{
    public class Enigma2
    {
        public string IPAddress { get; private set; }
        public string StreamingPort { get; private set; }

        private static DateTime _baseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public Enigma2(string ipAddress, string streamingPort)
        {
            IPAddress = ipAddress;
            StreamingPort = streamingPort;
        }

        public DateTime? ConvertDateTime(string enigma2Time)
        {
            DateTime? result = null;
            long dt;
            if (!string.IsNullOrEmpty(enigma2Time) && long.TryParse(enigma2Time, out dt))
            {
                result = _baseDateTime.AddSeconds(dt).ToLocalTime();
            }
            return result;
        }

        public async Task<e2settingslist> GetSettings()
        {
            e2settingslist result = null;
            await Task.Run(() =>
            {
                var s = GetWebIfResult("settings");
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2settingslist));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        result = (e2settingslist)serializer.Deserialize(reader);
                    }
                }
            });
            return result;
        }

        public async Task Zap(e2service service)
        {
            await Task.Run(() =>
            {
                var parameters = new Dictionary<string, string>();
                parameters.Add("sRef", service.e2servicereference);
                var s = GetWebIfResult("zap", parameters);
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2simplexmlresult));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        var result = (e2simplexmlresult)serializer.Deserialize(reader);
                    }
                }
            });
        }

        public async Task<e2currentserviceinformation> GetCurrentserviceinformation()
        {
            e2currentserviceinformation result = null;
            await Task.Run(() =>
            {
                var s = GetWebIfResult("getcurrent");
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2currentserviceinformation));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        result = (e2currentserviceinformation)serializer.Deserialize(reader);
                    }
                }
            });
            return result;
        }

        public async Task<e2powerstate> GetPowerState()
        {
            e2powerstate result = null;
            await Task.Run(() =>
            {
                var s = GetWebIfResult("powerstate");
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2powerstate));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        result = (e2powerstate)serializer.Deserialize(reader);
                    }
                }
            });
            return result;
        }

        public async Task<e2powerstate> ToggleStandby()
        {
            e2powerstate result = null;
            await Task.Run(() =>
            {
                var parameters = new Dictionary<string, string>();
                parameters.Add("newstate", "0");
                var s = GetWebIfResult("powerstate", parameters);
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2powerstate));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        result = (e2powerstate)serializer.Deserialize(reader);
                    }
                }
            });
            return result;
        }

        public async Task<e2servicelist> GetServices()
        {
            return await GetServices("");
        }

        public async Task<e2servicelist> GetServices(e2service service)
        {
            return await GetServices(service.e2servicereference);
        }

        public async Task<e2servicelist> GetServices(string sRef)
        {
            e2servicelist result = null;
            await Task.Run(() =>
            {
                Dictionary<string, string> parameters = null;
                if (!string.IsNullOrEmpty(sRef))
                {
                    parameters = new Dictionary<string, string>();
                    parameters.Add("sRef", sRef);
                }
                var s = GetWebIfResult("getservices", parameters);
                if (s != null)
                {
                    var serializer = new XmlSerializer(typeof(e2servicelist));
                    using (var stream = new StringReader(s))
                    using (var reader = XmlReader.Create(stream))
                    {
                        result = (e2servicelist)serializer.Deserialize(reader);
                    }
                }
            });
            return result;
        }

        public string GetM3UContent(string sref)
        {
            return $"#EXTM3U\r\n#EXTVLCOPT--http-reconnect=true\r\nhttp://{IPAddress}:{StreamingPort}/{sref}";
        }

        private string GetWebIfResult(string relPath, Dictionary<string, string> parameters = null)
        {
            string result = null;
            try
            {
                string url = GetWebIfUrl(relPath, parameters);
                System.Net.WebRequest webRequest = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
                using (System.IO.StreamReader responseReader = new System.IO.StreamReader(webRequest.GetResponse().GetResponseStream()))
                {
                    // and read the response
                    result = responseReader.ReadToEnd();
                }
            }
            catch
            {
            }
            return result;
        }

        private string RootWebIfUrl
        {
            get { return $"http://{IPAddress}/web/"; }
        }

        private string GetWebIfUrl(string relPath, Dictionary<string, string> parameters = null)
        {
            string result = RootWebIfUrl + HttpUtility.UrlEncode(relPath);
            if (parameters != null && parameters.Count > 0)
            {
                result += "?";
                foreach (var kp in parameters)
                {
                    if (!result.EndsWith("?"))
                    {
                        result += "&";
                    }
                    result += kp.Key + "=" + HttpUtility.UrlEncode(kp.Value);
                }
            }
            return result;
        }
    }
}
