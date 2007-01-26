/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
#region Using directives

#define USE_TRACING

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;

#endregion

/////////////////////////////////////////////////////////////////////
// <summary>contains GDataRequest, our thin wrapper class for request/response
// </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>constants for the authentication handler
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class GoogleAuthentication
    {
        /// <summary>Google client authentication handler</summary>
        public const string UriHandler = "https://www.google.com/accounts/ClientLogin"; 
        /// <summary>Google client authentication email</summary>
        public const string Email = "Email";
        /// <summary>Google client authentication password</summary>
        public const string Password = "Passwd";
        /// <summary>Google client authentication source constant</summary>
        public const string Source = "source";
        /// <summary>Google client authentication default service constant</summary>
        public const string Service = "service";
        /// <summary>Google client authentication LSID</summary>
        public const string Lsid = "LSID";
        /// <summary>Google client authentication SSID</summary>
        public const string Ssid = "SSID";
        /// <summary>Google client authentication Token</summary>
        public const string AuthToken = "Auth"; 
        /// <summary>Google authSub authentication Token</summary>
        public const string AuthSubToken = "Token"; 
        /// <summary>Google client header</summary>
        public const string Header = "Authorization: GoogleLogin auth="; 
        /// <summary>Google method override header</summary>
        public const string Override = "X-HTTP-Method-Override"; 
        /// <summary>Google webkey identifier</summary>
        public const string WebKey = "X-Google-Key: key=";
        /// <summary>Google webkey identifier</summary>
        public const string AccountType = "accountType=HOSTED_OR_GOOGLE";


    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequestFactory implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataGAuthRequestFactory : GDataRequestFactory
    {
        /// <summary>this factory's agent</summary> 
        public const string GDataGAuthAgent = "GDataGAuth-CS/1.0.0";
        private string gAuthToken;   // we want to remember the token here
        private string handler;      // so the handler is useroverridable, good for testing
        private string gService;         // the service we pass to Gaia for token creation
        private string applicationName;  // the application name we pass to Gaia and append to the user-agent
        private bool fMethodOverride;    // to override using post, or to use PUT/DELETE
        private int numberOfRetries;        // holds the number of retries the request will undertake
        private bool fStrictRedirect;       // indicates if redirects should be handled strictly

        
                                         

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataGAuthRequestFactory(string service, string applicationName) : base(applicationName)
        {
    	    this.Service = service;
    	    this.ApplicationName = applicationName;
    	    if (applicationName != null) {
    	        this.UserAgent = applicationName + " " + GDataGAuthAgent;
    	    } else {
    	        this.UserAgent = GDataGAuthAgent;
    	    }
            this.numberOfRetries = 0; 
            this.fStrictRedirect = false; 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataGAuthRequestFactory(string service, string applicationName, string library) : this(service, library)
        {
    	    if (applicationName != null) {
    	        this.UserAgent = applicationName + " " + this.UserAgent;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget)
        {
            return new GDataGAuthRequest(type, uriTarget, this); 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Get/Set accessor for gAuthToken</summary> 
        //////////////////////////////////////////////////////////////////////
        internal string GAuthToken
        {
            get {return this.gAuthToken;}
            set {
                Tracing.TraceMsg("set token called with: " + value); 
                this.gAuthToken = value;
                }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Get/Set accessor for the application name</summary> 
        //////////////////////////////////////////////////////////////////////
        public string ApplicationName
        {
            get {return this.applicationName == null ? "" : this.applicationName;}
            set {this.applicationName = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>returns the service string</summary> 
        //////////////////////////////////////////////////////////////////////
        public string Service
        {
            get {return this.gService;}
            set {this.gService = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public bool MethodOverride</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool MethodOverride
        {
            get {return this.fMethodOverride;}
            set {this.fMethodOverride = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>indicates if a redirect should be followed on not HTTPGet</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool StrictRedirect
        {
            get {return this.fStrictRedirect;}
            set {this.fStrictRedirect = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// property accessor to adjust how often a request of this factory should retry
        /// </summary>
        public int NumberOfRetries
        {
            get { return this.numberOfRetries; }
            set { this.numberOfRetries = value; }
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Handler</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Handler
        {
            get {

                return this.handler!=null ? this.handler : GoogleAuthentication.UriHandler; 
            }
            set {this.handler = value;}
        }
        /////////////////////////////////////////////////////////////////////////////
        
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequest implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataGAuthRequest : GDataRequest
    {
        /// <summary>holds the input in memory stream</summary> 
        private MemoryStream requestCopy;
        /// <summary>holds the factory instance</summary> 
        private GDataGAuthRequestFactory factory; 

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        internal GDataGAuthRequest(GDataRequestType type, Uri uriTarget, GDataGAuthRequestFactory factory)  : base(type, uriTarget, factory as GDataRequestFactory)
        {
            // need to remember the factory, so that we can pass the new authtoken back there if need be
            this.factory = factory; 
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>returns the writable request stream</summary> 
        /// <returns> the stream to write into</returns>
        //////////////////////////////////////////////////////////////////////
        public override Stream GetRequestStream()
        {
            this.requestCopy = new MemoryStream(); 
            return this.requestCopy; 
        }
        /////////////////////////////////////////////////////////////////////////////

       //////////////////////////////////////////////////////////////////////
       /// <summary>Read only accessor for requestCopy</summary> 
       //////////////////////////////////////////////////////////////////////
       internal Stream RequestCopy
       {
           get {return this.requestCopy;}
       }
       /////////////////////////////////////////////////////////////////////////////
       

        
        //////////////////////////////////////////////////////////////////////
        /// <summary>does the real disposition</summary> 
        /// <param name="disposing">indicates if dispose called it or finalize</param>
        //////////////////////////////////////////////////////////////////////
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); 
            if (this.disposed == true)
            {
                return;
            }
            if (disposing == true)
            {
                if (this.requestCopy != null)
                {
                    this.requestCopy.Close();
                    this.requestCopy = null;
                }
                this.disposed = true;
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>sets up the correct credentials for this call, pending 
        /// security scheme</summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void EnsureCredentials()
        {
            Tracing.Assert(this.Request!= null, "We should have a webrequest now"); 
            if (this.Request == null)
            {
                return; 
            }
            // if the token is NULL, we need to get a token. 
            if (this.factory.GAuthToken == null)
            {
                // we will take the standard credentials for that
                NetworkCredential nc = this.Credentials as NetworkCredential;
                Tracing.TraceMsg(nc == null ? "No Network credentials set" : "Network credentials found"); 
                if (nc != null)
                {
                    // only now we have something to do... 
                    this.factory.GAuthToken = QueryAuthToken(nc); 
                }
            }
            // now add the auth token to the header
            // Tracing.Assert(this.factory.GAuthToken != null, "We should have a token now"); 
            Tracing.TraceMsg("Using auth token: " + this.factory.GAuthToken); 
            string strHeader = GoogleAuthentication.Header + this.factory.GAuthToken; 
            this.Request.Headers.Add(strHeader); 
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>sets the redirect to false after everything else
        /// is done </summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void EnsureWebRequest()
        {
            base.EnsureWebRequest(); 
            HttpWebRequest http = this.Request as HttpWebRequest; 
            if (http != null)
            {
                // we do not want this to autoredirect, our security header will be 
                // lost in that case
                http.AllowAutoRedirect = false;
                if (this.factory.MethodOverride == true && 
                    http.Method != HttpMethods.Get &&
                    http.Method != HttpMethods.Post)
                {
                    // not put and delete, all is post
                    if (http.Method == HttpMethods.Delete)
                    {
                        http.ContentLength = 0;
                        // to make this NOT crash under .NET CF, get the request stream
                        // and close it again
                        Stream req = http.GetRequestStream(); 
                        req.Close(); 
                    }
                    http.Headers.Add(GoogleAuthentication.Override, http.Method);
                    http.Method = HttpMethods.Post; 
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>goes to the Google auth service, and gets a new auth token</summary> 
        /// <returns>the auth token, or NULL if none received</returns>
        //////////////////////////////////////////////////////////////////////
        protected string QueryAuthToken(NetworkCredential nc)
        {
            Tracing.Assert(nc != null, "Do not call QueryAuthToken with no network credentials"); 
            if (nc == null)
            {
                throw new System.ArgumentNullException("nc", "No credentials supplied");
            }
            // Create a new request to the authentication URL.    
            Uri authHandler = null; 
            try 
            {
                authHandler = new Uri(this.factory.Handler); 
            }
            catch
            {
                throw new GDataRequestException("Invalid authentication handler URI given"); 
            }

            WebRequest authRequest = WebRequest.Create(authHandler); 
            if (this.factory.Proxy != null)
            {
                authRequest.Proxy = this.factory.Proxy; 
            }
            HttpWebRequest web = authRequest as HttpWebRequest;
            if (web != null)
            {
                web.KeepAlive = this.factory.KeepAlive; 
            }
            WebResponse authResponse = null; 

            string authToken = null; 
            try
            {
                authRequest.ContentType = HttpFormPost.Encoding; 
                authRequest.Method = HttpMethods.Post;
                ASCIIEncoding encoder = new ASCIIEncoding();

                // now enter the data in the stream
                string postData = GoogleAuthentication.Email + "=" + nc.UserName + "&"; 
                postData += GoogleAuthentication.Password + "=" + nc.Password + "&";  
                postData += GoogleAuthentication.Source + "=" + this.factory.ApplicationName + "&"; 
                postData += GoogleAuthentication.Service + "=" + this.factory.Service + "&"; 
                postData += GoogleAuthentication.AccountType; 

                byte[] encodedData = encoder.GetBytes(postData);
                authRequest.ContentLength = encodedData.Length; 

                Stream requestStream = authRequest.GetRequestStream() ;
                requestStream.Write(encodedData, 0, encodedData.Length); 
                requestStream.Close();        
                authResponse = authRequest.GetResponse(); 

            } 
            catch (WebException e)
            {
                Tracing.TraceMsg("QueryAuthtoken failed " + e.Status); 
                throw new GDataRequestException("Execution of authentication request failed", e);
            }
            HttpWebResponse response = authResponse as HttpWebResponse;
            if (response != null)
            {
                int code= (int)response.StatusCode;
                if (code != 200)
                {
                    throw new GDataRequestException("Execution of authentication request returned unexpected result: " +code,  this.Response); 
                }
                // check the content type, it must be text
                if (!response.ContentType.StartsWith(HttpFormPost.ContentType))
                {
                    throw new GDataRequestException("Execution of authentication request returned unexpected content type: " + response.ContentType,  this.Response); 
                }
                // verify the content length. This should not be big, hence a big result might indicate a phoney
                if (response.ContentLength > 1024)
                {
                    throw new GDataRequestException("Execution of authentication request returned unexpected large content length: " + response.ContentLength,  this.Response); 
                }

                authToken = Utilities.ParseValueFormStream(response.GetResponseStream(), GoogleAuthentication.AuthToken); 
            }
            Tracing.Assert(authToken != null, "did not find an auth token in QueryAuthToken");
            if (authResponse != null)
            {
                authResponse.Close();
            }
            return authToken;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Executes the request and prepares the response stream. Also 
        /// does error checking</summary> 
        //////////////////////////////////////////////////////////////////////
        public override void Execute()
        {
            // call him the first time
            Execute(1); 
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>Executes the request and prepares the response stream. Also 
        /// does error checking</summary> 
        /// <param name="iRetrying">indicates the n-th time this is run</param>
        //////////////////////////////////////////////////////////////////////
        protected void Execute(int iRetrying)
        {
            Tracing.TraceCall("GoogleAuth: Execution called");
            try
            {
                CopyRequestData();
                base.Execute();
            }
            catch (GDataForbiddenException re) 
            {
                Tracing.TraceMsg("need to reauthenticate, got a forbidden back");
                // do it again, once, reset AuthToken first and streams first
                base.Reset();
                this.factory.GAuthToken = null; 
                CopyRequestData();
                base.Execute();

            }
            catch (GDataRedirectException re)
            {
                // we got a redirect.
                Tracing.TraceMsg("Got a redirect to: " + re.Location);
                // only reset the base, the auth cookie is still valid
                // and cookies are stored in the factory
                if (this.factory.StrictRedirect == true)
                {
                    HttpWebRequest http = this.Request as HttpWebRequest; 
                    if (http != null)
                    {
                        // only redirect for GET, else throw
                        if (http.Method != HttpMethods.Get) 
                        {
                            throw re; 
                        }
                    }
                }
                base.Reset();
                this.TargetUri = new Uri(re.Location);
                CopyRequestData();
                base.Execute();
            }
            catch (GDataRequestException re)
            {
                if (iRetrying > this.factory.NumberOfRetries)
                {
                    Tracing.TraceMsg("Got no response object");
                    throw re;
                }
                Tracing.TraceMsg("Let's retry this"); 
                // only reset the base, the auth cookie is still valid
                // and cookies are stored in the factory
                base.Reset();
                this.Execute(iRetrying + 1); 
            }
            catch (Exception e)
            {
                Tracing.TraceMsg("we caught an unknown exception");
                throw e; 
            }
            finally
            {
                if (this.requestCopy != null)
                {
                    this.requestCopy.Close();
                    this.requestCopy = null;
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>takes our copy of the stream, and puts it into the request stream</summary> 
        //////////////////////////////////////////////////////////////////////
        protected void CopyRequestData()
        {
            if (this.requestCopy != null)
            {
                // Since we don't use write buffering on the WebRequest object,
                // we need to ensure the Content-Length field is correctly set
                // to the length we want to set.
                base.EnsureWebRequest();
                base.Request.ContentLength = this.requestCopy.Length;
                // stream it into the real request stream
                Stream req = base.GetRequestStream();

                const int size = 4096;
                byte[] bytes = new byte[4096];
                int numBytes;

                this.requestCopy.Seek(0, SeekOrigin.Begin); 

                while((numBytes = this.requestCopy.Read(bytes, 0, size)) > 0)
                {
                    req.Write(bytes, 0, numBytes);
                }
                req.Close();
            }
        }
        /////////////////////////////////////////////////////////////////////////////

    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////