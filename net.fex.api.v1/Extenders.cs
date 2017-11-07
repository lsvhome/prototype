using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Fex.Api
{
    public static class Extenders
    {
        internal static void Process(this Exception exception)
        {
            System.Diagnostics.Debugger.Break();
        }

        internal static Uri AppendQuery(this Uri uri, string key, string value)
        {
            UriBuilder uriBuilder = new UriBuilder(uri);
            if (string.IsNullOrWhiteSpace(uriBuilder.Query))
            {
                uriBuilder.Query = string.Format("{0}={1}", key, System.Net.WebUtility.UrlEncode(value));
            }
            else
            {
                uriBuilder.Query = uriBuilder.Query.TrimStart('?') + string.Format("&{0}={1}", key, System.Net.WebUtility.UrlEncode(value));
            }

            return uriBuilder.Uri;
        }
    }
}
