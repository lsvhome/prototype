using System;

namespace FexSync.Data
{
    public abstract class FilePathChangedEventArgs : EventArgs
    {
        public string OldPath { get; set; }

        public string NewPath { get; set; }
    }
}
