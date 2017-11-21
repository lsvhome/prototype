using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsolePrivateTest
{
    public class PrivateContainer
    {
        private void SupportMethod(HttpWebRequest request)
        {
            Console.WriteLine();
        }

        private HttpWebRequest PrivateMethod()
        {
            var ret = (HttpWebRequest)HttpWebRequest.Create(new Uri("https://fex.net"));
            this.SupportMethod(ret);
            return ret;
        }

        public HttpWebRequest Exec()
        {
            return this.PrivateMethod();
        }
    }
}
