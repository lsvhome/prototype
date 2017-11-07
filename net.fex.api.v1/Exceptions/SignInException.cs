using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public class SignInException : ConnectionException
    {
        public SignInException(string message, int id) : base(message)
        {
            this.Id = id;
        }

        public int Id { get; set; }
    }
}
