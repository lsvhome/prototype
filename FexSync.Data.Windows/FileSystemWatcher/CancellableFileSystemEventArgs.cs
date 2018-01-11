using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FexSync
{
    public class CancellableFileSystemEventArgs : EventArgs
    {
        public CancellableFileSystemEventArgs(FileSystemEventArgs fileSystemEventArgs)
        {
            this.FileSystemEventArgs = fileSystemEventArgs;
        }

        public FileSystemEventArgs FileSystemEventArgs { get; private set; }

        public bool Suppress { get; set; } = false;
    }
}
