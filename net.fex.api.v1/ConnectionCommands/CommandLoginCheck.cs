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
        public CommandLoginCheck(string login) : base(
            new Dictionary<string, string>
            {
                { "login", login }
            })
        {
        }

        protected override string Suffix => "j_login_check";

        public override void Execute(IConnection connection)
        {
            try
            {
                base.Execute(connection);
                this.Result = true;
            }
            catch (ApiErrorException ex)
            {
                if (ex.ResponseResult.Error.Id == 1019)
                {
                    this.Result = false;
                }
                else
                {
                    throw;
                }
            }
        }

        public bool Result { get; private set; }
    }
}
