using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FexSync.Data
{
    public class CaptchaRequiredAlert : Alert
    {
        public Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs CaptchaRequestedEventArgs { get; private set; }

        private System.Threading.EventWaitHandle WaitHandle { get; set; }

        public CaptchaRequiredAlert(Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs captchaRequestedEventArgs, System.Threading.EventWaitHandle waitHandle)
        {
            this.Priority = AlertPriority.critical;
            this.CaptchaRequestedEventArgs = captchaRequestedEventArgs;
            this.WaitHandle = waitHandle;

            this.Text = "Captcha Input Required";
        }

        public override void MarkProcessed()
        {
            base.MarkProcessed();
            this.WaitHandle.Set();
        }
    }
}
