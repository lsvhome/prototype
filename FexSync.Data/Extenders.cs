using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace FexSync.Data
{
    public static class Extenders
    {
        internal static void Process(this Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception.ToString());
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
        }

        public static string Sha1(this FileInfo fileInfo)
        {
            using (FileStream fs = fileInfo.OpenRead())
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (SHA1Managed sha1 = new SHA1Managed())
                    {
                        byte[] hash = sha1.ComputeHash(bs);
                        StringBuilder formatted = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                        {
                            formatted.AppendFormat("{0:X2}", b);
                        }

                        return formatted.ToString().ToLower();
                    }
                }
            }
        }

        public static int ToUnixTime(this DateTime dateTime)
        {
            var unixTimestamp = dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return (int)unixTimestamp;
        }

        public static DateTime FromUnixTime(this int unixTimestamp)
        {
            System.DateTime unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            var ret = unixStartTime
                .AddSeconds(unixTimestamp)
                .ToLocalTime();
            return ret;
        }

        public static T Parse<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (var r = new StringReader(xml))
            {
                var ret = (T)serializer.Deserialize(r);
                return ret;
            }
        }

        public static string Save<T>(T data)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringBuilder ret = new StringBuilder();
            using (var w = new StringWriter(ret))
            {
                serializer.Serialize(w, data);
            }

            return ret.ToString();
        }

        public static IEnumerable<Net.Fex.Api.CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject> FilterUniqueNames(this IEnumerable<Net.Fex.Api.CommandBuildRemoteTree.CommandBuildRemoteTreeItemObject> list)
        {
            /*
             * If list contains items with unique names result should contain only one item, with max UploadTime
             */
            var filtered =
                list.GroupBy(x => x.Object.Name.ToLower())
                .Select(itemsWithIdenticalName => itemsWithIdenticalName.OrderByDescending(item => item.Object.UploadTime).First());

            return filtered;
        }
    }
}
