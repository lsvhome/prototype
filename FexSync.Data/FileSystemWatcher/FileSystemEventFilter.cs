using System;
using System.IO;

namespace FexSync.Data
{
    public class FileSystemEventFilter
    {
        public DateTime StopMoment { get; set; }

        public Func<bool> Completed { get; set; }

        public Func<FileSystemEventArgs, bool> FilterConditionShouldSuppress { get; set; }
    }
}
