using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsolePrivateTest
{
    public class FexUploadHttpRequestCreator : IWebRequestCreate
    {
        public static Action<HttpWebRequest> CustomInitAfterCreate = null;

        public static event Action<HttpWebRequest> OnCustomInitAfterCreate;

        public WebRequest Create(Uri Uri)
        {
            HttpWebRequest ret = Activator.CreateInstance(typeof(HttpWebRequest),
                                        BindingFlags.CreateInstance | BindingFlags.Public |
                                        BindingFlags.NonPublic | BindingFlags.Instance,
                                        null, new object[] { Uri, null }, null) as HttpWebRequest;

            if (CustomInitAfterCreate != null)
            {
                CustomInitAfterCreate((HttpWebRequest)ret);
            }

            return ret;
        }
    }
}