using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandSignIn : CommandBaseUnAuthorizedUser
    {
        /*
         
"Если все ок: {""user"":{""login"":""dvzdvz"",""priv"":0},""result"":1}
При ошибке:
{""captcha"":0,""err"":{""id"":1024,""msg"":""Неверный логин или пароль.""},""result"":0}"

             */
        public CommandSignIn(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        protected override string Suffix => "j_signin";

        public User Result
        {
            get
            {
                if (this.ResultJObject.Value<int>("result") == 1)
                {
                    JObject jUser = this.ResultJObject.Value<JObject>("user");
                    var ret = new User(jUser.Value<string>("login"), jUser.Value<int>("priv"));
                    return ret;
                }
                else
                {
                    JObject jErr = this.ResultJObject.Value<JObject>("err");
                    string message = jErr.Value<string>("msg");
                    int id = jErr.Value<int>("id");
                    string captcha = this.ResultJObject.Value<string>("captcha");
                    throw new SignInException(message, id) { ErrorCode = 5001 };
                }
            }
        }
    }
}