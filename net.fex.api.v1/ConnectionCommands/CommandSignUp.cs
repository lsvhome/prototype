using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandSignUp : CommandCaptchaRequestPossible
    {
        /*
"step   1:   При   отправке   только   телефона,   высылается   код   на   номер   телефона
{captcha:   1,   err:   {msg:   ""Проверочное   слово   указано   неверно."",   id:   1008},   result:   0}
Если   ""captcha"":1
      phone:380961234567   
      captcha_token:6fO19yIEAQ2fBlAXnoyfcCx2OKOQhbWr   //   токен   капчи,   создается   рандомно   (0-9   A-Z   a-z)
      captcha_value:182   //   код   с   картинки

   возможные   ошибки:
         1009:   ""singup_used_phone"":   ""Номер   телефона   уже   зарегистрирован"",
         1010:   ""singup_attemps"":   ""Слишком   много   запросов   для   этого   номера   телефона""
step   2:   При   отправке   логина,   регистрирует   в   БД   новый   логин
   возможные   ошибки:
         1012:   ""singup_wrong_code"":   ""Неверный   код   подтверждения"",
         1008:   ""singup_wrong_captcha"":   ""Проверочное   слово   указано   неверно"",
         1019:   ""singup_used_login"":   ""Логин   уже   зарегистрирован"",
         1016:   ""singup_login_with_letter"":   ""Логин   может   начинаться   только   с   буквы"""                           

            
            
https://fex.net/j_signup?phone=380683662836
{"captcha":0,"result":1}


*/

        public CommandSignUp(string phone) : base(
            new Dictionary<string, string>
            {
                { "phone", phone }
            })
        {
        }

        public CommandSignUp(string code, string password, string login, string phone, string mail) : base(
            new Dictionary<string, string>
            {
                { "code", code },
                { "password", password },
                { "login", login },
                { "phone", phone },
                { "mail", mail }
            })
        {
        }

        protected override string Suffix => "j_signup";
    }
}
