using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandSignUp: CommandBaseUnAuthorizedUser
    {
        /*
"step 1: При отправке только телефона, высылается код на номер телефона
{captcha: 1, err: {msg: ""Проверочное слово указано неверно."", id: 1008}, result: 0}
Если ""captcha"":1
		phone:380961234567 
		captcha_token:6fO19yIEAQ2fBlAXnoyfcCx2OKOQhbWr // токен капчи, создается рандомно (0-9 A-Z a-z)
		captcha_value:182 // код с картинки

	возможные ошибки:
		 1009: ""singup_used_phone"": ""Номер телефона уже зарегистрирован"",
		 1010: ""singup_attemps"": ""Слишком много запросов для этого номера телефона""
step 2: При отправке логина, регистрирует в БД новый логин
	возможные ошибки:
		 1012: ""singup_wrong_code"": ""Неверный код подтверждения"",
		 1008: ""singup_wrong_captcha"": ""Проверочное слово указано неверно"",
		 1019: ""singup_used_login"": ""Логин уже зарегистрирован"",
		 1016: ""singup_login_with_letter"": ""Логин может начинаться только с буквы"""         
             
             */
        public CommandSignUp(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        protected override string Suffix => "j_signup";

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
                    var ex = new SignInException(message, id) { ErrorCode = 5003 };
                    throw ex;
                }
            }
        }

        public override void Execute(IConnection connection)
        {
            base.Execute(connection);

            if (this.ResultJObject.Value<int>("result") == 1)
            {
                if (this.ResultJObject.Value<int>("captcha") == 1)
                {
                    throw new CaptchaRequiredException();
                }
                //// Expected: {"captcha":0,"result":1}
                return;
            }
            else
            {
                //// Expected: 
                //// { captcha: 1, err: { msg: ""Проверочное слово указано неверно."", id: 1008}, result: 0}
                //// { "result":0,"err":{ "msg":"Номер телефона указан неверно.","id":1007},"captcha":0}
                //// { "captcha":0,"result":0,"err":{"id":1010,"msg":"Слишком много запросов для этого номера телефона."}}

                if (this.ResultJObject.Value<int>("captcha") == 1)
                {
                    throw new CaptchaRequiredException();
                }

                JObject jErr = this.ResultJObject.Value<JObject>("err");
                string message = jErr.Value<string>("msg");
                int id = jErr.Value<int>("id");
                throw new SignInException(message, id) { ErrorCode = 5004 };
            }
        }

    }
}
