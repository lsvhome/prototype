using System;
using System.Collections.Generic;
using System.Text;

namespace net.fex.api.v1
{
    public class User
    {
        public User(string login, int priv)
        {
            this.Login = login;
            this.priv = priv;
        }

        public string Login { get; private set; }

        public int priv { get; private set; }
    }
}
