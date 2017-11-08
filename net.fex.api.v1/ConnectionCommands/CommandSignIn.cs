using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandSignIn : CommandCaptchaRequestPossible
    {
        [DataContract]
        public class CommandSignInResult
        {
            [DataMember]
            public int Result { get; set; }

            [DataMember]
            public User User { get; set; }
        }

        /*
         
"Если все ок: {""user"":{""login"":""dvzdvz"",""priv"":0},""result"":1}

{"user":{"login":"slutai","priv":0,"info":{"max_expire_time":0,"upload_count":1,"pay_upload_size":879,
"pay_upload_count":1,"system_id":0,"upload_size":879,"max_upload_size":0}},"result":1}


При ошибке:
{""captcha"":0,""err"":{""id"":1024,""msg"":""Неверный логин или пароль.""},""result"":0}"



             */

        [DataContract]
        public class Info
        {
            [DataMember(Name = "max_expire_time")]
            public int MaxExpireTime { get; set; }

            [DataMember(Name = "upload_count")]
            public int UploadCount { get; set; }

            [DataMember(Name = "pay_upload_size")]
            public int PayUploadSize { get; set; }

            [DataMember(Name = "pay_upload_count")]
            public int PayUploadCount { get; set; }

            [DataMember(Name = "system_id")]
            public int SystemId { get; set; }

            [DataMember(Name = "upload_size")]
            public int UploadSize { get; set; }

            [DataMember(Name = "max_upload_size")]
            public int MaxUploadSize { get; set; }
        }

        [DataContract]
        public class User
        {
            public User(string login, int priv)
            {
                this.Login = login;
                this.Priv = priv;
            }

            [DataMember]
            public string Login { get; private set; }

            [DataMember]
            public int Priv { get; private set; }

            [DataMember]
            public Info Info { get; set; }
        }

        public CommandSignIn(string login, string password, bool stay_signed) : base(
            new Dictionary<string, string>
            {
                { "login", login },
                { "password", password },
                { "stay_signed", stay_signed ? "1" : "0" }
            })
        {
        }

        public CommandSignIn(string login, string password, bool stay_signed, string captcha_token, string captcha_value) : base(
            new Dictionary<string, string>
            {
                { "login", login },
                { "password", password },
                { "stay_signed", stay_signed ? "1" : "0" },
                { "captcha_token", captcha_token },
                { "captcha_value", captcha_value },
            })
        {
        }

        protected override string Suffix => "j_signin";

        public CommandSignIn.CommandSignInResult Result
        {
            get
            {
                var ret = this.ResultJObject.ToObject<CommandSignIn.CommandSignInResult>();
                return ret;
            }
        }
    }
}