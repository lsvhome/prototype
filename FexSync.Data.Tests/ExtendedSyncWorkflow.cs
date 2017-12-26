using System;
using System.Threading.Tasks;

namespace FexSync.Data.Tests
{
    public class ExtendedSyncWorkflow : SyncWorkflow
    {
        public ExtendedSyncWorkflow(SyncWorkflowConfig config) : base()
        {
            this.Reconfigure(config);
        }

        public void StartForOneIterationAndStop(TimeSpan timeout)
        {
            using (System.Threading.AutoResetEvent waiter = new System.Threading.AutoResetEvent(false))
            {
                EventHandler c = (object sender, EventArgs e) =>
                {
                    waiter.Set();
                };

                this.OnTransferFinished += c;
                this.Start();
                waiter.WaitOne();
                this.OnTransferFinished -= c;
                this.Stop();
            }
        }
    }
}
