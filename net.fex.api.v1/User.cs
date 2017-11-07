using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public class User
    {
        public User(string login, int priv)
        {
            this.Login = login;
            this.Priv = priv;
        }

        public string Login { get; private set; }

        public int Priv { get; private set; }
    }
}
