using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DeltaSphereTestApp.Helpers
{
    public static class MyDeltaresHelper
    {
        public static string TryGetSession(this string url, Uri baseUri)
        {
            var authUrl = new Uri(baseUri, "auth");
            return url.ToLower().StartsWith(authUrl.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase)
                ? authUrl.GetGlobalCookies()
                : null;
        }

        public static bool IsEndUrl(this string uri)
        {
            return uri.ToLower().StartsWith("https://sts.deltares.nl/adfs/ls/", StringComparison.InvariantCultureIgnoreCase);
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetGetCookieEx(string pchUrl, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);

        internal static string GetGlobalCookies(this Uri uri)
        {
            uint datasize = 2048;
            const int internetCookieHttponly = 0x00002000;

            var cookieData = new StringBuilder((int)datasize);

            if (InternetGetCookieEx(uri.AbsoluteUri, null, cookieData, ref datasize, internetCookieHttponly, IntPtr.Zero)
                && cookieData.Length > 0)
            {
                return cookieData.ToString().Replace(';', ',');
            }

            return null;
        }
    }
}