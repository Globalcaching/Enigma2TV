using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Enigma2TV
{
    public class WebApiController : ApiController
    {
        // GET api/values/5 
        public string Get(string id)
        {
            switch (id)
            {
                case "up":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Up);
                    break;
                case "enter":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Enter);
                    break;
                case "down":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Down);
                    break;
                case "left":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Left);
                    break;
                case "right":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Right);
                    break;
                case "e":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.E);
                    break;
                case "esc":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.Escape);
                    break;
                case "s":
                    MainWindow.Instance.SimulateKeyDown(System.Windows.Input.Key.S);
                    break;
            }
            return "OK";
        }
    }
}
