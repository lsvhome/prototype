using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandCaptcha : CommandBase
    {
        public class CommandCaptchaResult : IDisposable
        {
            public Image Image { get; set; }

            public string Token { get; set; }

            public void Dispose()
            {
                if (this.Image != null)
                {
                    this.Image.Dispose();
                    this.Image = null;
                }
            }
        }

        public CommandCaptcha() : this(GenerateNewToken())
        {
        }

        private CommandCaptcha(string captcha_token) : base(
            new Dictionary<string, string>
            {
                { "captcha_token", captcha_token }
            })
        {
        }

        protected override string Suffix => "captcha";

        public CommandCaptchaResult Result { get; private set; }

        /// <summary>
        /// generate new captcha token with length of 32 symbols
        /// </summary>
        public static string GenerateNewToken()
        {
            var random = new Random();
            StringBuilder token = new StringBuilder();

            var az = System.Linq.Enumerable.Range('a', 'z' - 'a' + 1);
            var digits = System.Linq.Enumerable.Range('0', '9' - '0' + 1);

            var allChars =
                az.Select(ch => (char)ch)
                .Union(az.Select(ch => ch.ToString().ToUpper().First()))
                .Union(digits.Select(ch => (char)ch))
                .ToArray();

            for (int i = 0; i < 32; i++)
            {
                token.Append(allChars[random.Next(0, allChars.Length)]);
            }

            return token.ToString();
        }

        public override void Execute(IConnection connection)
        {
            try
            {
                var uri = this.BuildUrl(connection);
                System.Diagnostics.Debug.WriteLine(uri);

                using (var s = connection.Client.GetStreamAsync(uri).Result)
                {
                    this.Result = new CommandCaptchaResult { Image = System.Drawing.Image.FromStream(s), Token = this.Parameters["captcha_token"] };
                }
            }
            catch (ConnectionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ConnectionException(ex.Message, ex) { ErrorCode = 5008 };
            }
        }
    }
}
