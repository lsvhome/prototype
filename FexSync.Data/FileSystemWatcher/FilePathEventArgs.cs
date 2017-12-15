using System;

namespace FexSync.Data
{
    public abstract class FilePathEventArgs : EventArgs
    {
        public string FullPath { get; set; }
    }
}
