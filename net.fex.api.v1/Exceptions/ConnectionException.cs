using System;
using System.Collections.Generic;
using System.Text;

namespace net.fex.api.v1
{
    public class ConnectionException : Exception
    {
        public ConnectionException()
        {
        }

        public ConnectionException(string message) : base(message)
        {
        }

        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public int ErrorCode { get; set; }

        public string HttpResponse { get; set; }
    }
}
