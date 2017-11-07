using System;
using System.Collections.Generic;
using System.Text;

namespace net.fex.api.v1
{
    public class LoginException : ConnectionException
    {
        public LoginException(string message, int id) : base(message)
        {
            this.Id = id;
        }

        public int Id { get; set; }
    }
}
