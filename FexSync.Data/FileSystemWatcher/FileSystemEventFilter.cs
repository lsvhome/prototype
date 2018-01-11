using System;
using System.IO;

namespace FexSync.Data
{
    public class FileSystemEventFilter
    {
        public Func<FileSystemEventArgs, bool> FilterConditionShouldSuppress { get; private set; }

        public FileSystemEventFilter(Func<FileSystemEventArgs, bool> filterConditionShouldSuppress)
        {
            if (filterConditionShouldSuppress == null)
            {
                throw new ArgumentNullException("filterConditionShouldSuppress");
            }

            this.FilterConditionShouldSuppress = filterConditionShouldSuppress;
        }
    }
}
