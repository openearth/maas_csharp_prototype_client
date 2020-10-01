using System;
using System.Net;

namespace DeltaSphereTestApp.Helpers
{
    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            
            if (request is HttpWebRequest httpRequest)
            {
                httpRequest.CookieContainer = CookieContainer;
            }

            return request;
        }
    }
}