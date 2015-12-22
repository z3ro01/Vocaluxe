using System;
using System.Timers;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;
using VocaluxeLib;
using VocaluxeLib.Xml;
using System.Web.Script.Serialization;

namespace VocaluxeLib.Community
{
    public static class CCommunity
    {
        private static Boolean alreadySended = false;
        private static SComQueryScore currentScores;
        public static SCommunityConfig config;
        private static Boolean initalized = false;
        public static Boolean connectionStatus = false;
        public static Boolean loginStatus = false;
        private static SComAuthUser currentUser;
        //every 2 minute
        private static Timer checkTimer = new Timer(120000);
      
        public static void Init()
        {
            currentUser = new SComAuthUser();
            LoadConfig();

            if (!initalized)
            {
                checkTimer.Elapsed += (timerSender, timerEvent) => checkConnection(timerSender, timerEvent);
                checkTimer.Enabled = true;
                checkTimer.Start();
            }
            initalized = true;
        }

        public static void LoadConfig()
        {
            config = CBase.Config.GetCommunityConfig();
            RunEventHandlers(EComEventType.settingsChanged, 1);
           
        }

        public static void checkConnection(Object sender = null, ElapsedEventArgs e = null)
        {
            if (isEnabled() && isReadyForAuth())
            {
                login(delegate(SComResponseAuth response)
                {
                    //valid
                    if (response.status == 1)
                    {
                        currentUser.sessionId = response.sessionId;
                        
                        if (!currentUser.authenticated) {
                            currentUser.authenticated = true;
                            currentUser.displayName = response.displayName;
                            currentUser.avatarUrl = response.avatarUrl;
                            loginStatus = true;
                            RunEventHandlers(EComEventType.loginStatus, 1);
                        }
                    }
                    else if (response.status == 0)
                    {
                        //invalid credentials
                        if (response.code == 2)
                        {
                            logout();
                        }
                        //other server error
                        else
                        {
                            //do nothing
                        }
                    }
                    //no connection
                    else {
                        RunEventHandlers(EComEventType.connectionStatus, 0);
                    }
                });
            }
        }

        public static void login(Action<SComResponseAuth> callback)
        {
            SComQueryAuth data = new SComQueryAuth();
            data.username = config.AuthUser;
            data.password = config.AuthUUID;
            data.sessionId = currentUser.sessionId;
            authWithUUIDAsync(data, (Action<SComResponseAuth>)callback);
        }

        public static Boolean isEnabled()
        {
            if (config.Active == EOffOn.TR_CONFIG_ON)
            {
                if (!string.IsNullOrEmpty(config.Server))
                {
                    return true;
                }
            }
            return false;
        }

        public static int findMainProfile()
        {
            var profiles = CBase.Profiles.GetProfiles();
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i].CommunityUsername == config.AuthUser) //&& profiles[i].CommunityToken == config.AuthUUID
                {
                    currentUser.profileCreated = profiles[i].ID;
                    return profiles[i].ID;
                }
            }
            return -1;
        }

        public static Boolean isReadyForAuth()
        {
            if (!string.IsNullOrEmpty(config.AuthUser) && !string.IsNullOrEmpty(config.AuthUUID))
            {
                return true;
            }
            return false;
        }

        public static Boolean isLoggedIn()
        {
            return loginStatus;
        }

        public static void saveAuthData(String Username, String uuid)
        {
            config.AuthUser  = Username;
            config.AuthUUID = uuid;
            CBase.Config.SetCommunityConfig(config);
        }

        public static void setCurrentUser(SComAuthUser data)
        {
            currentUser = data;
            RunEventHandlers(EComEventType.loginStatus, 1);
            loginStatus = true;
        }

        public static void logout()
        {
            currentUser = new SComAuthUser();
            currentUser.authenticated = false;

            saveAuthData(null, null);
            config.AuthUser = null;
            config.AuthUUID = null;

            loginStatus = false;
            RunEventHandlers(EComEventType.loginStatus, 0);
        }

        public static SComAuthUser getCurrentUser()
        {
            return currentUser;
        }

        public static Boolean CanAutoSendScores()
        {
            if (isEnabled())
            {
                if (config.AutosendScores == EOffOn.TR_CONFIG_ON)
                {
                    return true;
                }
            }
            return false;
        }

        public static void SetScores(SComQueryScore sc)
        {
            alreadySended = false;
            currentScores = sc;
        }

        public static void clearScores()
        {
            alreadySended = false;
            currentScores = new SComQueryScore();
        }

        public static string getName()
        {
            if (string.IsNullOrEmpty(config.Name))
            {
                return "Unknown server";
            }
            else
            {
                return config.Name;
            }
        }

        public static bool CanSendScoresNow()
        {
            if (currentScores.scores != null)
            {
                if (currentScores.scores.Length > 0)
                {
                    if (!alreadySended)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool getAlreadySended()
        {
            return alreadySended;
        }

        #region Get data from server
        public static void authRequestAsync(SComQueryAuth data, Action<SComResponseAuth, SComQueryAuth> callback)
        {
            RunEventHandlers(EComEventType.loading, 1);
            var task = Task.Factory.StartNew(() => _authRequestAsync(data, callback));
        }

        public static void authWithUUIDAsync(SComQueryAuth data, Action<SComResponseAuth> callback)
        {
            RunEventHandlers(EComEventType.loading, 1);
            var task = Task<SComResponseAuth>.Factory.StartNew(() => _authWithUUID(data, callback));
        }

        public static SComResponseAuth authWithUUID(SComQueryAuth data, Action<SComResponseAuth> callback = null)
        {
            RunEventHandlers(EComEventType.loading, 1);
            return _authWithUUID(data, callback);
        }

        public static void getNewsAsync(Action<SComSongsResult> callback)
        {
            RunEventHandlers(EComEventType.loading, 1);
            var data = new SComQueryCmd();
            data.method = "getnews";
            var task = Task<SComSongsResult>.Factory.StartNew(() => _getSongs(data, callback));
        }

        public static void getSongsAsync(SComQueryCmd query, Action<SComSongsResult> callback)
        {
            RunEventHandlers(EComEventType.loading, 1);
            var task = Task<SComSongsResult>.Factory.StartNew(() => _getSongs(query, callback));
        }

        public static void getTextAsync(string url, Action<String> callback = null)
        {
            var task = Task<String>.Factory.StartNew(() => _getText(url, callback));
        }

        public static void sendScoreAsync(Action<SComResponse> callback)
        {
            var task = Task<SComResponse>.Factory.StartNew(() => _sendScoreAsync(callback));
        }
        public static void getScoresAsync(SComQueryHighScores data, Action<SComResultScore> callback)
        {
            var task = Task<SComResultScore>.Factory.StartNew(() => _getScoresAsync(data, callback));
        }

        /*public static void getContestsAsync(Action<SComResultContestList> callback)
        {
            var task = Task<SComResultContestList>.Factory.StartNew(() => _getContestsAsync(callback));
        }
        public static void getCPlaylistAsync(int contestId, Action<SComResultCPlaylist> callback)
        {
            var task = Task<SComResultCPlaylist>.Factory.StartNew(() => _getCPlaylistAsync(contestId, callback));
        }
        public static void getContestAccessAsync(string[] profileFiles, int contestId, Action<SComResponse> callback)
        {
            var task = Task<SComResponse>.Factory.StartNew(() => _getContestAccessAsync(profileFiles, contestId, callback));
        }*/
        #endregion

        #region Workers

        private static void _authRequestAsync(SComQueryAuth data, Action<SComResponseAuth, SComQueryAuth> callback)
        {
            SComQueryCmd request = new SComQueryCmd();
            request.method = "vcx-handshake";
            request.version = config.Version;
            request.lang = config.Lang;


            SComResponseAuth result = new SComResponseAuth();
            SComWebResponse response = Request(toJSON(request));
            if (response.status == 1)
            {
                SComResultHandshake jresult;
                if (parseJSON(response.rawdata, out jresult) == false) {
                    result.status = 0;
                    result.message = "Wrong response from server...";
                    callback(result, data);
                    RunEventHandlers(EComEventType.loading, 0);
                    return;
                }else {
                    if (data.method == null) 
                        data.method = "auth";
                    data.version = config.Version;
                    data.lang = config.Lang;
                    //server request rsa encrypting
                    if (jresult.key > 0 && jresult.pubkey != null)
                    {
                        var pwdtoencode = Encoding.UTF8.GetBytes(data.password);
                        using (var rsa = new RSACryptoServiceProvider())
                        {
                            rsa.FromXmlString(jresult.pubkey);
                            var encodedB64 = Convert.ToBase64String(rsa.Encrypt(pwdtoencode, false));
                            data.password = encodedB64;
                            data.key = jresult.key;
                        }
                    }

                    SComResponseAuth authResult = new SComResponseAuth();
                    SComWebResponse responseAuth = Request(toJSON(data));
                    if (responseAuth.status == 0)
                    {
                        authResult.message = responseAuth.message!=null?responseAuth.message:"Unknown error";
                        authResult.status = 0;
                    }
                    else
                    {
                        if (parseJSON(responseAuth.rawdata, out result) == false)
                        {
                            authResult.status = 0;
                            authResult.message = "Wrong response from server...";
                        }
                    }
                    callback(result, data);
                    RunEventHandlers(EComEventType.loading, 0);
                    return;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                callback(result, data);
                return;
            }
        }

        private static SComResponseAuth _authWithUUID(SComQueryAuth data, Action<SComResponseAuth> callback = null)
        {
            if (data.method == null) 
                data.method = "auth-uuid";
            data.key = 0;
            data.version = config.Version;
            data.lang = config.Lang;

            SComResponseAuth result = new SComResponseAuth();
            SComWebResponse response = Request(toJSON(data));
            if (response.status == 1)
            {
                SComResponseAuth jresult;
                if (parseJSON(response.rawdata, out jresult) == false)
                {
                    result.status = 0;
                    result.message = "Wrong response from server...";
                }
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                
                return jresult;
            }
            else
            {
                result.status = -1;
                result.message = response.message;
                if (callback != null) { callback(result); }
                RunEventHandlers(EComEventType.loading, 0);
                return result;
            }
        }

        private static SComSongsResult _getSongs(SComQueryCmd data, Action<SComSongsResult> callback = null)
        {
            data.sessionId = currentUser.sessionId;
            if (currentUser.sessionId == null)
            {
                data.username = currentUser.username;
                data.password = currentUser.uuid;
            }
            data.version = config.Version;
            data.lang = config.Lang;

            SComWebResponse response = Request(toJSON(data));
            if (response.status == 1)
            {
                SComSongsResult jresult;
                if (parseJSON(response.rawdata, out jresult) == false)
                {
                    jresult.status = 0;
                    jresult.message = "Wrong response from server...";
                }
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
            else
            {
                SComSongsResult jresult = new SComSongsResult();
                jresult.status = -1;
                jresult.message = response.message;
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
        }

        private static string _getText(string url, Action<string> callback = null)
        {
            String rawdata = String.Empty;
            var request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        rawdata = reader.ReadToEnd();
                    }

                    if (callback != null)
                        callback(rawdata);

                    return rawdata;
                }
            }
            catch (WebException e)
            {
                if (callback != null)
                    callback(String.Empty);
                return String.Empty;
            }
            catch (Exception e)
            {
                if (callback != null)
                    callback(String.Empty);
                return String.Empty;
            }
        }

        private static SComResponse _sendScoreAsync(Action<SComResponse> callback)
        {
            currentScores.lang = config.Lang;
            currentScores.version = config.Version;
            currentScores.sessionId = currentUser.sessionId;
            currentScores.username  = currentUser.username;
            if (currentUser.sessionId == null)
                currentScores.uuid = currentUser.uuid;

            if (String.IsNullOrEmpty(currentScores.method)) { 
                currentScores.method = "setscores";
            }

            SComWebResponse response = Request(toJSON(currentScores));
            if (response.status == 1)
            {
                SComResponse jresult;
                if (parseJSON(response.rawdata, out jresult) == false)
                {
                    jresult.status = 0;
                    jresult.message = "Wrong response from server...";
                }
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
            else
            {
                SComResponse jresult = new SComResponse();
                jresult.status = -1;
                jresult.message = response.message;
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
        }

        private static SComResultScore _getScoresAsync(SComQueryHighScores data, Action<SComResultScore> callback)
        {
            data.version = config.Version;
            data.lang = config.Lang;

            if (String.IsNullOrEmpty(data.method))
            {
                data.method = "getscores";
            }
            SComWebResponse response = Request(toJSON(data));
            if (response.status == 1)
            {
                SComResultScore jresult;
                if (parseJSON(response.rawdata, out jresult) == false)
                {
                    jresult.status = 0;
                    jresult.message = "Wrong response from server...";
                }
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
            else
            {
                SComResultScore jresult = new SComResultScore();
                jresult.status = -1;
                jresult.message = response.message;
                if (callback != null) { callback(jresult); }
                RunEventHandlers(EComEventType.loading, 0);
                return jresult;
            }
        }

        /*private static SComResultContestList _getContestsAsync(Action<SComResultContestList> callback)
        {
            
        }

        private static SComResultCPlaylist _getCPlaylistAsync(int contestId, Action<SComResultCPlaylist> callback)
        {
           
        }

        private static SComResponse _getContestAccessAsync(string[] profileFiles, int contestId, Action<SComResponse> callback)
        {
           
        }*/

        #endregion

        #region HTTP request, serialization

        private static string toJSON<T>(T obj)
        {
            var json = new JavaScriptSerializer().Serialize(obj);
            // string json = JsonConvert.SerializeObject(obj);
            return json;
        }

        private static bool parseJSON<T>(String jsonstring, out T obj)
        {
            obj = default(T);
            if (jsonstring == null) { return false; }
            try
            {
                obj = new JavaScriptSerializer().Deserialize<T>(jsonstring);
                //obj = JsonConvert.DeserializeObject<T>(jsonstring);
                return true;
            }
           /* catch (Newtonsoft.Json.JsonReaderException e)
            {
                CBase.Log.LogError("CommunityClient: JSON Parse error:  " + e.Message);
                CBase.Log.LogError("CommunityClient: Original response: " + jsonstring);
                return false;
            }*/
            catch (Exception e)
            {
                CBase.Log.LogError("CommunityClient: Server response error:  " + e.Message);
                CBase.Log.LogError("CommunityClient: Original response: " + jsonstring);
                return false;
            }
        }

        private static SComWebResponse Request(string requestData)
        {
            SComWebResponse result = new SComWebResponse();
            var request = (HttpWebRequest)WebRequest.Create(config.Server);
            request.Timeout = 10000;
            request.ContentType = "application/json";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "POST";

            StreamReader streamReader = null;
            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(requestData);
                    streamWriter.Flush();
                    streamWriter.Close();
                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.OK) {
                        using (streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            result.status = 1;
                            result.rawdata = streamReader.ReadToEnd();

                            if (connectionStatus == false)
                            {
                                connectionStatus = true;
                                RunEventHandlers(EComEventType.connectionStatus, 1);
                            }
                            return result;
                        }
                    }
                    else
                    {
                        connectionStatus = false;
                        RunEventHandlers(EComEventType.connectionStatus, 0);
                        result.status = -1;
                        result.message = httpResponse.StatusCode.ToString()+ " / "+httpResponse.StatusDescription.ToString();
                        return result;
                    }
                }
            }
            catch (WebException e)
            {
                result.status  = 0;
                result.message = e.Message.ToString();
                connectionStatus = false;
                RunEventHandlers(EComEventType.connectionStatus, 0);
                return result;
            }
            catch (Exception e)
            {
                result.status   = 0;
                result.message = e.Message.ToString();
                connectionStatus = false;
                RunEventHandlers(EComEventType.connectionStatus, 0);
                return result;
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
            }
        }

        #endregion

        #region File downloader functions

        private static Action<SComDownloadStatus> _dlProgressCallback;
        private static Action<SComDownloadStatus> _dlDoneCallback;
        private static WebClient _dlWebclient;

        public static void checkRemoteFileAsync(string url, Action<SComDLHeaders> callback)
        {
            var task = Task<SComDLHeaders>.Factory.StartNew(() => checkRemoteFile(url, callback));
        }

        public static SComDLHeaders checkRemoteFile(string url, Action<SComDLHeaders> callback = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Timeout = 10000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "HEAD";
            request.AllowAutoRedirect = true;
            SComDLHeaders callbackData = new SComDLHeaders();
            callbackData.status = 0;
            callbackData.headers = new List<KeyValuePair<String, String>>();
           /* try
            {*/
                var httpResponse = (HttpWebResponse)request.GetResponse();
                if (httpResponse.Headers.Count > 0)
                {
                    foreach (string header in httpResponse.Headers)
                    {
                        if (header.ToLower().Equals("content-type"))
                        {
                            callbackData.contentType = httpResponse.Headers[header];
                        }
                        else if (header.ToLower().Equals("content-length"))
                        {
                            callbackData.contentLength = int.Parse(httpResponse.Headers[header]);
                        }
                        callbackData.headers.Add(new KeyValuePair<String, String>(header, httpResponse.Headers[header]));
                    }
                    callbackData.status = 1;
                }
                if (callback != null) { callback(callbackData); }
                return callbackData;
           /* }*/
          /*  catch
            {
                if (callback != null) { callback(callbackData); }
                return callbackData;
            }*/
        }

        public static bool downloadFile(string url, string path)
        {
            WebClient Client = new WebClient();
            try
            {
                Client.DownloadFile(new Uri(url), path);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static void downloadFileAsync(string url, string path, Action<SComDownloadStatus> progressCallback, Action<SComDownloadStatus> doneCallback)
        {
            _dlProgressCallback = progressCallback;
            _dlDoneCallback = doneCallback;
            _dlWebclient = new WebClient();
            _dlWebclient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_dlProgress);
            _dlWebclient.DownloadFileCompleted += new AsyncCompletedEventHandler(_dlDone);
            _dlWebclient.DownloadFileAsync(new Uri(url), path);
        }

        public static void cancelDownloadAsync()
        {
            _dlWebclient.CancelAsync();
        }

        private static void _dlDone(object sender, AsyncCompletedEventArgs e)
        {
            SComDownloadStatus status = new SComDownloadStatus();
            status.Error        = 0;
            status.Done         = true;

            if (_dlWebclient.ResponseHeaders != null) {
                status.HttpHeaders = new List<KeyValuePair<string, string>>();
                foreach (string key in _dlWebclient.ResponseHeaders)
                {
                    status.HttpHeaders.Add(new KeyValuePair<string, string>(key, _dlWebclient.ResponseHeaders[key] ));
                }

                if (e.Error != null)
                {
                    status.Error = 1;
                }
            }
            else
            {
                status.Error = 1;
            }
            if (_dlDoneCallback != null)
                _dlDoneCallback(status);
        }

        private static void _dlProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            SComDownloadStatus status = new SComDownloadStatus();
            status.BytesReceived = e.BytesReceived;
            status.TotalBytes = e.TotalBytesToReceive;
            status.Percentage = e.ProgressPercentage;

            if (_dlProgressCallback != null)
                _dlProgressCallback(status);
        }

        #endregion


        public static string hashTextFile(string path, string filename)
        {
            byte[] ubyte = File.ReadAllBytes(Path.Combine(path, filename));
            byte[] newbyte = new byte[ubyte.Length];
            int z = 0;
            Boolean waitnextline = false;

            for (int i = 0; i < ubyte.Length; i++)
            {
                //skip newlines (cr lf)
                if (ubyte[i] == 13 || ubyte[i] == 10)
                {
                    if (ubyte[i] == 10) { waitnextline = false; }
                }
                else
                {
                    //skip header lines (starts with #)
                    if (ubyte[i] == 35)
                    {
                        waitnextline = true;
                    }
                    if (waitnextline == false)
                    {
                        newbyte[z] = ubyte[i];
                        z++;
                    }
                }
            }

            //resize bytearray for valid hash
            byte[] hashbytes = new byte[z];
            for (int e = 0; e < z; e++)
            {
                hashbytes[e] = newbyte[e];
            }

            return _hashBytes(hashbytes);
        }

        public static string MD5Hash(string input)
        {
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            return _hashBytes(inputBytes);
        }

        private static string _hashBytes(byte[] s)
        {
            byte[] hashedBytes = MD5CryptoServiceProvider.Create().ComputeHash(s);
            string hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            return hash;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        #region Eventhandlers
        public struct evHandler
        {
            public EComEventType[] type;
            public Action<SComEvent> callback;
        }

        private static List<evHandler> eventHandlers;
        public static void RemoveAllEventHandler()
        {
            eventHandlers = new List<evHandler>();
        }

        public static void AddEventHandler(EComEventType[] eventType, Action<SComEvent> callable)
        {
            if (eventHandlers == null) { eventHandlers = new List<evHandler>(); }
            evHandler current = new evHandler();
            current.type = eventType;
            current.callback = callable;
            eventHandlers.Add(current);
        }

        public static void AddEventHandler(Action<SComEvent> callable)
        {
            if (eventHandlers == null) { eventHandlers = new List<evHandler>(); }
            evHandler current = new evHandler();
            current.type = new EComEventType[4] { EComEventType.connectionStatus, EComEventType.loginStatus, EComEventType.settingsChanged, EComEventType.loading };
            current.callback = callable;
            eventHandlers.Add(current);
        }

        private static void RunEventHandlers(EComEventType eventType, int status = 0)
        {
            SComEvent eventCall = new SComEvent();
            eventCall.eventType = eventType;
            eventCall.status = status;

            eventHandlers.ForEach(delegate(evHandler eventData)
            {
                for (int i=0; i < eventData.type.Length; i++) {
                    if (eventData.type[i] == eventType) {
                         eventData.callback(eventCall);
                    }
                }
            });
        }

        #endregion
    }

    #region Structs

    public enum EComEventType {
        connectionStatus = 0,
        loginStatus = 1,
        settingsChanged = 2,
        loading = 4
    }

    public struct SComEvent
    {
        public EComEventType eventType;
        public int status;
    }

    public struct SComDLHeaders
    {
        public int status;
        public string contentType;
        public int contentLength;
        public List<KeyValuePair<string, string>> headers;
    }


    public struct SComDownloadStatus
    {
        public double BytesReceived;
        public double TotalBytes;
        public float Percentage;
        public int Error;
        public bool Done;
        public List<KeyValuePair<string,string>> HttpHeaders;
        public string ContentType;
    }

    public enum EComResultType
    {
        Login = 0,
        CmdStatus = 1,
        ScoreStatus = 2,
        HighScores = 3,
        Ping = 4,
        ContestList = 5
    }

    public enum EComQueryType
    {
        Cmd = 0,
        SendScore = 1
    }

    public struct SComQueryAuth
    {
        public string version;
        public string lang;
        public string method;
        public string username;
        public string password;
        public string sessionId;
        public int key;
        public Dictionary<string, string> parameters;

    }

    public struct SComQueryCmd
    {
        public string version;
        public string lang;
        public string method;
        public string username;
        public string password;
        public string sessionId;
        public Dictionary<string, string> parameters;
    }

//NEW

    public struct SComResultListInfo
    {
        public int count;
        public int start;
        public int end;
        public int limit;
    }

    public struct SComSongsResult
    {
        public int status;
        public string message;
        public SComRemoteSong[] items;
        public SComResultListInfo info;
    }

    public struct SComRemoteSong
    {
        public string txtHash;
        public string artist;
        public string title;
        public string type;
        public int remoteId;
        public string coverUrl;
        public string[] knownHashes;
        public bool hasDownloadSupport;
        public bool creditBasedDownloads;
        public int credit;
        public Dictionary<string, Dictionary<string,string>> fileList;
        public string licenceUrl;
    }

    public struct SComQueryHighScores
    {
        public string version;
        public string lang;
        public string username;
        public string uuid;
        public string sessionId;
        public string method;
        public int difficulty;
        public int id;
        public string txthash;
        public string gameMode;
        public string partyMode;
        public string queryType;
        public Dictionary<string, string> parameters;
    }

    public struct SComQueryScore
    {
        public string version;
        public string lang;
        public string method;
        public string gameMode;
        public string partyMode;
        public string username;
        public string uuid;
        public string sessionId;
        public int guests;
        public int id;
        public SComQueryScoreItem[] scores;
    }

    public class SComQueryScoreItem
    {
        public string username;
        public string uuid;
        public string playerName;
        public int playerId;
        public int round;
        public double score;
        public double goldenbonus;
        public double linebonus;
        public string gameMode;
        public string difficulty;
        public string txtHash;
        public string artist;
        public string title;
        public int voicenr;
    }

    public class SComQueryCommunityContest
    {


    }

    public struct SComWebResponse
    {
        public int status;
        public string rawdata;
        public string message;
    }

    public struct SComRemoteProfile
    {
        public string displayName;
        public string avatarUrl;
        public string username;
        public string profileType;
        public string uuid;
    }

    public struct SComResponseGeneral
    {
        public int status;
        public int code;
        public string type;
        public string message;
    }

    public struct SComResponseAuth
    {
        public int status;
        public int code;
        public string message;
        public string displayName;
        public string avatarUrl;
        public string username;
        public string profileType;
        public string uuid;
        public string sessionId;
    }

    public struct SComAuthUser
    {
        public string displayName;
        public string avatarFile;
        public string avatarUrl;
        public string username;
        public string profileType;
        public string uuid;
        public int profileCreated;
        public Boolean authenticated;
        public string sessionId;
    }

    public struct SComResponse
    {
        public int status;
        public int code;
        public string message;
        public string result;
        public Dictionary<string, string> parameters;
    }

    public struct SComResultHandshake
    {
        public int status;
        public int key;
        public string pubkey;
    }

    public struct SComResultContestList
    {
        public int status;
        public int code;
        public string message;
        public List<SComResultContestItem> result;
    }

    public struct SComResultContestItem
    {
        public int id;
        public string name;
        public string mode;
        public int accessible;
        public DateTime startDate;
        public EGameMode gameMode;
        public EGameDifficulty difficulty;
        public string description;
    }

    public struct SComResultCPlaylist
    {
        public int status;
        public int code;
        public int playlistId;
        public string message;
        public List<SComResultCPlaylistItem> result;
    }

    public struct SComResultCPlaylistItem
    {
        public string artist;
        public string title;
        public string hash;
        public string txtUrl;
        public string[] files;
        public int localId;
        public int remoteId;
    }

    public struct SComResultScore
    {
        public SComResultScoreList result;
        public int status;
        public string message;
        public int code;
    }

    public struct SComResultScoreList
    {
        public int lastrefresh;
        public List<SComResultScoreItem> easy;
        public List<SComResultScoreItem> medium;
        public List<SComResultScoreItem> hard;
    }

    public struct SComResultScoreItem
    {
        public string Name;
        public string Difficulty;
        public string Score;
        public string VoiceNr;
        public string Date;
    }

    #endregion
}
