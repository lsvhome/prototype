using System;

namespace FexSync.Data
{
    public abstract class Alert
    {
        public class AlertEventArgs : EventArgs
        {
            public Alert Alert { get; set; }
        }

        public enum AlertPriority
        {
            critical = 0,

            high = 20,

            medium = 40,

            low = 60
        }

        public enum AlertScope
        {
            session = 0,

            persistent = 10
        }

        public int Id { get; set; }

        public AlertPriority Priority { get; set; } = AlertPriority.medium;

        public AlertScope Scope { get; set; } = AlertScope.session;

        public virtual string Text { get; protected set; }

        public bool Processed { get; set; } = false;

        public virtual void MarkProcessed()
        {
            this.Processed = true;
        }
    }
}
