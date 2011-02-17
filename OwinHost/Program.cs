using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owin {
    class Program {
        public static void Main(string[] args) {
            //fake http request - PUT /spec.html HTTP/1.1
            //this request would actually replace the spec with the string "Hello World"
            //header and body request are just straight strings, host actually does the parsing
            var host = new Wilson(Console.Out);
            host.Add(new SimpleApplication().OnRequest);
            //host.Add(new ChunkedApplication().OnRequest);
            host.Request(
                "GET",  //method
                "/spec.html",  //uri
                "host:owin.github.com\naccept:application/json\nuseragent:Mozilla/5.0 (X11; U; SunOS sun4u; en-US; rv:0.9.4.1) Gecko/20020406 Netscape6/6.2.2", //header string
                "Hello World" //body string
            );
        }
    }
}
