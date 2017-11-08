using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public abstract class CommandCaptchaRequestPossible : CommandBaseUnAuthorizedUser
    {
        protected internal CommandCaptchaRequestPossible(IDictionary<string, string> parameters) : base(parameters)
        {
        }

        public class CaptchaRequestedEventArgs : EventArgs
        {
            public CaptchaRequestedEventArgs(CommandCaptcha.CommandCaptchaResult captcha)
            {
                this.Captcha = captcha;
            }

            public CommandCaptcha.CommandCaptchaResult Captcha { get; private set; }

            public string CaptchaText { get; set; }
        }

        public override void Execute(IConnection connection)
        {
            try
            {
                base.Execute(connection);
            }
            catch (CaptchaRequiredException)
            {
                using (var captchaResult = connection.Captcha())
                {
                    string text = connection.CaptchaGetUserInput(captchaResult);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        this.Parameters.Add("captcha_token", captchaResult.Token);
                        this.Parameters.Add("captcha_value", text);

                        base.Execute(connection);
                        return;
                    }
                }

                throw;
            }
        }
    }
}