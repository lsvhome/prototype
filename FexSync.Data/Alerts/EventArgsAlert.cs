using System;

namespace FexSync.Data
{
    public class EventArgsAlert : Alert
    {
        public EventArgs EventArgs { get; private set; }

        public EventArgsAlert(EventArgs eventArgs, string text)
        {
            this.EventArgs = eventArgs;
            this.Text  = text;
        }
    }
}
