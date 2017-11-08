using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandSignOut : CommandBaseAuthorizedUser
    {
        public CommandSignOut() : base(new Dictionary<string, string>())
        {
        }

        protected override string Suffix => "j_signout";
    }
}
