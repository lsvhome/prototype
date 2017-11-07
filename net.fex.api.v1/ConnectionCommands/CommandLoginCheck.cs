using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandLoginCheck : CommandBaseUnAuthorizedUser
    {
        /*
         
"Если все ОК: {""result"":1}
Если логин занят: {""result"":0,""err"":{""msg"":""Логин уже зарегистрирован."",""id"":1019}} "

             */
        public CommandLoginCheck(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        protected override string Suffix => "j_login_check";

        public bool Result
        {
            get
            {
                if (this.ResultJObject.Value<int>("result") == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
