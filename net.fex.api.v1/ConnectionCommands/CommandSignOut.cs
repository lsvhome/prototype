using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Net.Fex.Api
{
    public class CommandSignOut : CommandBaseAuthorizedUser
    {
        protected override string Suffix => "j_signout";

        public override void Execute(IConnection connection)
        {
            base.Execute(connection);

            //// Expected: {"result":1}

            if (this.ResultJObject.Value<int>("result") == 1)
            {
                return;
            }
            else
            {
                throw new ConnectionException() { ErrorCode = 5002 };
            }
        }
    }
}
