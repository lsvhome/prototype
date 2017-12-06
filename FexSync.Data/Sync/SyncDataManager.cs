using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Fex.Api
{
    public class SyncDataManager
    {
        private static string DataCacheFullPath
        {
            get
            {
                return Path.Combine(System.Configuration.ConfigurationManager.AppSettings["DataFolder"], "syncdata.cache");
            }
        }

        private static System.Xml.Serialization.XmlSerializer CreateSerializer()
        {
            return new System.Xml.Serialization.XmlSerializer(typeof(SyncData));
        }

        public static void Save(SyncData data)
        {
            using (var stream = File.OpenWrite(DataCacheFullPath))
            {
                CreateSerializer().Serialize(stream, data);
            }
        }

        public static SyncData Load()
        {
            using (var stream = File.OpenRead(DataCacheFullPath))
            {
                var loaded = (SyncData)CreateSerializer().Deserialize(stream);
                return loaded;
            }
        }
    }
}
