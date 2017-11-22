using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;
using Net.Fex.Api;

namespace FexSync
{
    public class SyncWorkflow : IDisposable
    {
        public enum SyncWorkflowStatus
        {
            Started,
            Stopped
        }

        private static object lockObj = new object();

        private readonly System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

        public SyncWorkflow()
        {
            this.worker.WorkerSupportsCancellation = true;
            this.worker.DoWork += this.Worker_DoWork;
        }

        public void Start()
        {
            lock (lockObj)
            {
                if (this.status == SyncWorkflowStatus.Stopped && !this.worker.CancellationPending)
                {
                    this.status = SyncWorkflowStatus.Started;
                    this.worker.RunWorkerAsync();
                }
            }
        }

        public void Stop()
        {
            lock (lockObj)
            {
                if (this.worker.IsBusy)
                {
                    this.worker.CancelAsync();
                }
            }
        }

        public event EventHandler<Connection.ExceptionEventArgs>  OnException;

        public void WaitStoppped(TimeSpan timeout)
        {
            if (this.worker.CancellationPending)
            {
                System.Threading.AutoResetEvent waiter = new System.Threading.AutoResetEvent(false);
                waiter.Reset();
                this.worker.RunWorkerCompleted += (object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) => { waiter.Set(); };
                waiter.WaitOne();
            }
        }

        public void SyncOneFile(string token)
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        private SyncWorkflowStatus status = SyncWorkflowStatus.Stopped;
        
        public SyncWorkflowStatus Status
        {
            get
            {
                lock (lockObj)
                {
                    return this.status;
                }
            }
        }

        private void Connect_OnCaptchaUserInputRequired(object sender, Net.Fex.Api.CommandCaptchaRequestPossible.CaptchaRequestedEventArgs e)
        {
            // do nothing. We can't request user input
        }

        private void Worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                string login;
                string password;

                CredentialsManager.Load(out login, out password);

                using (var conn = ((App)App.Current).Container.Resolve<IConnectionFactory>().CreateConnection())
                {
                    conn.OnCaptchaUserInputRequired = this.Connect_OnCaptchaUserInputRequired;
                    var signin = conn.SignIn(login, password, false);
                    try
                    {
                        while (!this.worker.CancellationPending)
                        {
                            var x = this.BuildSyncList(conn);
                            System.Threading.Thread.Sleep(10000);
                        }
                    }
                    finally
                    {
                        conn.SignOut();
                        conn.OnCaptchaUserInputRequired = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.OnException != null)
                {
                    this.OnException(this, new Connection.ExceptionEventArgs(ex));
                }

                System.Diagnostics.Debug.Fail(ex.ToString());
            }
            finally
            {
                lock (lockObj)
                {
                    this.status = SyncWorkflowStatus.Stopped;
                }
            }
        }

        private readonly List<string> syncList = new List<string>();

        private static string appFolderName = "FEX.NET";

        private static string AppFolderFullPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName);
            }
        }

        private void BuildSyncList(IConnection conn)
        {
            CommandBuildRemoteTree.CommandBuildRemoteTreeResponse tree = conn.BuildRemoteTree();
        }
    }
}