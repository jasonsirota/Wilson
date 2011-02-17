using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Owin {
    class Wilson {

        //Run multiple applications on one request - NOT middleware
        private IList<
            Action <
                //environment dictionary containing server and request data
                IDictionary<string,object>,

                //response callback
                Action<string, IDictionary<string,IList<string>>, IEnumerable<object>>,

                //error callback
                Action<Exception>
            >
        > Applications = new List<Action<IDictionary<string,object>,Action<string, IDictionary<string,IList<string>>, IEnumerable<object>>,Action<Exception>>>();

        public TextWriter Output { get; set; }
        public string Body { get; set; }

        //constructor
        public Wilson(TextWriter output) {
            this.Output = output;
        }

        //fake request
        public Wilson Request(string method, string uri, string headers, string body){
            //opted to put Application as a required field rather than parameter input so multiple can be added
            if(Applications.Count == 0)
                throw new Exception("At least one application must be set explicitly before Request can be made to fake host");
            
            //create the environment
            var environment = CreateEnvironment(method, uri, headers);

            //create the delegates
            Action<string, IDictionary<string, IList<string>>, IEnumerable<object>> onResponse = OnResponse;
            Action<Exception> onError = OnError;

            //invoke the "application"
            foreach (var app in Applications)
                app(environment, onResponse, onError);

            //fluent
            return this;
        }

        public Wilson Add(Action<IDictionary<string, object>, Action<string, IDictionary<string, IList<string>>, IEnumerable<object>>, Action<Exception>> application) {
            this.Applications.Add(application);

            //fluent
            return this;
        }

        private IDictionary<string, object> CreateEnvironment(string method, string uri, string headers) {
            var headerDictionary = ParseHeaders(headers);
            Action<Action<ArraySegment<byte>>, Action<Exception>> onRequestBody = OnRequestBody;

            return new Dictionary<string, object>(){
                {"owin.RequestMethod",  method},
                {"owin.RequestUri",     uri},
                {"owin.RequestHeaders", headerDictionary},
                {"owin.RequestBody",    onRequestBody},
                {"owin.BaseUri",        uri},
                {"owin.ServerName",     headerDictionary["host"]},
                {"owin.ServerPort",     80},
                {"owin.UriScheme",      "http"},
                {"owin.RemoteEndPoint", null},
                {"owin.Version",        "1.0"}
            };
        }

        //TODO: In the spec, Dictionary in Response Callback section should be "IDictionary"
        private void OnResponse(string status, IDictionary<string, IList<string>> responseHeaders, IEnumerable<object> responseBody) {
            //just write the first two back to the output sync;
            Output.WriteLine("status: " + status);
            Output.WriteLine("headers:");
            foreach (var key in responseHeaders.Keys) {
                Output.Write("\t" + key + ":" + string.Join("\n\t\t",responseHeaders[key]) + "\n");
            }

            Output.WriteLine("body:");
            foreach (var bodySegment in responseBody) {
                //if the type is a string or byte aray just write it to the output
                if (bodySegment is byte[]) {
                    Output.Write(bodySegment);
                    continue;
                }

                //maybe put these in 1 if statement, not sure which is more readable
                if (bodySegment is ArraySegment<byte>) {
                    Output.Write(bodySegment);
                    continue;
                }

                //this one is a little more in-depth
                //TODO: make this better
                if (bodySegment is FileInfo) {
                    var fi = bodySegment as FileInfo;
                    if(fi != null) {
                        using(var reader = new StreamReader(fi.OpenRead())){
                            Output.Write(reader.ReadToEnd());
                        }
                    }
                    continue;
                }

                //if bodySegment is a delegate of a specific type it means write this
                //body to the response asynchronously, the delegate will tell you whether
                //to continue or there's an exception
                if (bodySegment is Action<Action<object>, Action<Exception>>) {
                    
                    //attempt to cast as the delegate
                    var bodySegmentDelegate = bodySegment as Action<Action<object>, Action<Exception>>;

                    //invoke the delegate exactly once from the host and tell the application what to do with the next chunk
                    if (bodySegmentDelegate != null)
                        bodySegmentDelegate(
                            //on next lambda
                            x => Output.Write(x),

                            //on exception lambda
                            ex => { throw ex; }
                        );

                    continue;
                }
            }
        }

        private void OnRequestBody(Action<ArraySegment<byte>> OnNext, Action<Exception> OnError) {
            try {
                //TODO: not sure why you need ArraySegment here, check with Benjamin, Louis
                var bodyBytes = UTF8Encoding.UTF8.GetBytes(this.Body);
                for (var i = 0; i < bodyBytes.Length; i += 32) {
                    OnNext(new ArraySegment<byte>(bodyBytes, i, 32));
                }
            } catch (Exception ex) {
                OnError(ex);
            }
        }

        private void OnError(Exception ex) {
            throw ex;
        }

        //TODO: in implementing can't use yield return on IDictionary<string,string>, IEnumerable<KVP<string,string>> perhaps.
        private static IDictionary<string, string> ParseHeaders(string headers) {
            var kvps = headers.Split('\n');
            var headerDictionary = new Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase);
            foreach(var kvp in kvps) {
                var nameValue = kvp.Split(":".ToCharArray(), 2, StringSplitOptions.None);
                headerDictionary.Add(nameValue[0], nameValue[1]);
            }
            return headerDictionary;
        }
    }
}
