using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace Owin {
    /// <summary>
    /// Stream the requested url out from the internet and into the console
    /// </summary>
    public class SimpleApplication {

        public void OnRequest(
            IDictionary<string, object> Environment,
            Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> onResponse,
            Action<Exception> onError) {

            try {
                //reconstruct the original url
                var requestHeaders = Environment["owin.RequestHeaders"] as IDictionary<string, string>;
                var uri = string.Format("{0}://{1}{2}",
                    Environment["owin.UriScheme"] as string,
                    Environment["owin.ServerName"] as string,
                    Environment["owin.RequestUri"] as string
                );

                //go out and grab the entire url and content synchronously
                var webRequest = HttpWebRequest.Create(uri);
                string content = "";
                var status = "200 OK";
                var responseHeaders = new Dictionary<string,IList<string>>();
                
                using(var response = webRequest.GetResponse() as HttpWebResponse){
                    status = ((int)response.StatusCode).ToString() + " " + response.StatusCode.ToString();
    
                    using(var contentReader = new StreamReader(response.GetResponseStream())) {
                        content = contentReader.ReadToEnd();
                    }

                    //build some response headers
                    foreach(var key in response.Headers.AllKeys){
                        var value = response.Headers[key];
                        responseHeaders.Add(key,new List<string>(){value});
                    }
                }

                //even though we just converted the bytes to a string, convert them back to a byte array
                var contentBytes = Encoding.UTF8.GetBytes(content);
                byte[][] arrayOfByteArray = {contentBytes};

                //now start the response;
                onResponse(
                    status,
                    responseHeaders,
                    arrayOfByteArray                    
                );
            } catch(Exception ex) {
                onError(ex);
            }
        }
    }
}