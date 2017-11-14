using Autofac;
using Net.Fex.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.Wpf
{
    public class SyncWorkflow : IDisposable
    {
        public enum SyncWorkflowStatus
        {
            Started,
            Stopped
        }

        System.ComponentModel.BackgroundWorker worker = new System.ComponentModel.BackgroundWorker();

        private static object lockObj = new object();

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

        public void SyncOneFile(string token)
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public SyncWorkflowStatus status = SyncWorkflowStatus.Stopped;
        
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
                    conn.OnCaptchaUserInputRequired += Connect_OnCaptchaUserInputRequired;
                    var signin = conn.SignIn(login, password, false);
                    try
                    {
                        this.BuildSyncList(conn);
                        //System.Threading.Thread.Sleep(10000);
                    }
                    finally
                    {
                        conn.SignOut();
                        conn.OnCaptchaUserInputRequired -= Connect_OnCaptchaUserInputRequired;
                    }
                }
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

        private static string appFolderFullPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName); } }

        private void BuildSyncList(IConnection conn)
        {
            int offset = 0;
            const int limit = 1000;
            CommandArchive.CommandArchiveResponse list;
            do
            {
                list = conn.Archive(offset, limit);
                //foreach (var each in list.ObjectList)
                //{
                //    each.
                //    if (File.Exists())
                //    each.ModifyTime
                //        //syncList
                //}

            }
            while (list.Count == limit);
        }
    }
}