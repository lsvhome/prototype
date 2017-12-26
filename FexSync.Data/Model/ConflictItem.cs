using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FexSync.Data
{
    public class ConflictItem
    {
        public enum ConflictItemState
        {
            UnResolved = 0,
            UseRemote = 1,
            UseLocal = 2
        }

        [Key]
        public int ConflictItemId { get; set; }

        private string LocalFileOldXml { get; set; }

        [NotMapped]
        public LocalFileModified[] LocalModifications
        {
            get
            {
                return Extenders.Parse<LocalFileModified[]>(this.LocalFileOldXml);
            }

            set
            {
                this.LocalFileOldXml = Extenders.Save<LocalFileModified[]>(value);
            }
        }

        private string RemoteModificationsXml { get; set; }

        [NotMapped]
        public RemoteFileModified[] RemoteModifications
        {
            get
            {
                return Extenders.Parse<RemoteFileModified[]>(this.RemoteModificationsXml);
            }

            set
            {
                this.RemoteModificationsXml = Extenders.Save<RemoteFileModified[]>(value);
            }
        }

        [NotMapped]
        private string path;

        public string Path
        {
            get
            {
                return this.path;
            }

            set
            {
                this.path = value.Trim(System.IO.Path.DirectorySeparatorChar);
#if DEBUG
                if (System.IO.Path.IsPathRooted(this.path))
                {
                    throw new ApplicationException();
                }
#endif
            }
        }

        public ConflictItemState State { get; set; } = ConflictItemState.UnResolved;
    }
}
