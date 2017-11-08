using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Net.Fex.Api
{
    public class CommandCaptcha : CommandBase
    {
        protected override string Suffix => "captcha";

        public Image Result { get; private set; }

        public override void Execute(IConnection connection)
        {
            try
            {
                var uri = this.BuildUrl(connection);
                System.Diagnostics.Debug.WriteLine(uri);
                using (var response = connection.Client.GetAsync(uri).Result)
                {
                    using (var s = response.Content.ReadAsStreamAsync().Result)
                    {
                        this.Result = System.Drawing.Image.FromStream(s);
                    }
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
