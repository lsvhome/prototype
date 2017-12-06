using System;

namespace FexSync.Data.Tests
{
    public class ExtendedSyncWorkflow : SyncWorkflow
    {
        public ExtendedSyncWorkflow(SyncWorkflowConfig config) : base()
        {
            this.Reconfigure(config);
        }

        public void WaitForOneIterationAndStoppped(TimeSpan timeout)
        {
            if (this.worker != null)
            {
                using (System.Threading.AutoResetEvent waiter = new System.Threading.AutoResetEvent(false))
                {
                    waiter.Reset();
                    lock (SyncWorkflow.lockObj)
                    {
                        if (this.worker != null)
                        {
                            this.OnIterationFinished += (object sender, EventArgs e) => { this.Stop(); };
                            this.worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) => { waiter.Set(); };
                        }
                    }

                    waiter.WaitOne();
                }
            }
        }
    }
}
