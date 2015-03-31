using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Dynamic;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Xml.Serialization;
using VocaluxeLib;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Network
{
    public static class CCommunity
    {
        public static List<SComProfile> users = new List<SComProfile>();
        private static Boolean _hasXmlError;
        private static Boolean alreadySended = false;
        private static SComQueryScore currentScores;
        private static SCommunityConfig config;
        private static bool initalized = false;
        private static SComProfile authProfile;
        private static string version = "0.0.1";

        public static void Init()
        {
            LoadConfig();
        }
        public static void LoadConfig()
        {
            if (initalized == false)
            {
               config = CBase.Config.GetCommunityConfig();
            }
            initalized = true;
        }
        public static Boolean isEnabled()
        {
            LoadConfig();
            if (config.Active == EOffOn.TR_CONFIG_ON)
            {
                if (!string.IsNullOrEmpty(config.Server))
                {
                    return true;
                }
            }
            return false;
        }
        public static Boolean canAutoSend()
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
        public static void setScores(SComQueryScore sc)
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
        public static void loadProfiles()
        {
            LoadConfig();
            try
            {
                string[] nprofiles = Directory.GetFiles(Path.Combine(Path.Combine(CBase.Settings.GetDataPath(), "Community"), "Profiles"), "*.xml");
                if (nprofiles.Length > 0)
                {
                    for (int i = 0; i < nprofiles.Length; i++)
                    {
                        _hasXmlError = false;
                        var xml = new CXmlDeserializer(new CXmlErrorHandler(_gotXMLError));
                        if (_hasXmlError)
                        {
                            CBase.Log.LogError("CommunityClient: Cant load profile file: " + nprofiles[i]);
                        }
                        else
                        {
                            SComProfile current = xml.Deserialize<SComProfile>(nprofiles[i]);
                            if (!String.IsNullOrEmpty(current.Name) && !String.IsNullOrEmpty(current.Email) && !String.IsNullOrEmpty(current.Password))
                            {
                                current.ProfileFile = Path.GetFileName(nprofiles[i]);
                                current.currentId = i + 1;
                                users.Add(current);
                            }
                        }
                    }

                    if (!hasAuthProfile())
                    {
                        if (!String.IsNullOrEmpty(config.AuthProfile) && !String.IsNullOrWhiteSpace(config.AuthProfile))
                        {
                            authProfile = getProfileByFile(config.AuthProfile);
                            if (!hasAuthProfile())
                            {
                                authProfile = users[0];
                            }
                        }
                        else
                        {
                            authProfile = users[0];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CBase.Log.LogError("CommunityClient: " + e.Message);
            }
        }
        public static Boolean hasProfiles()
        {
            if (users.Count > 0) { return true; }
            return false;
        }
        public static List<SComProfile> getProfiles()
        {
            return users;
        }
        public static Boolean hasProfile(String email)
        {
            var value = users.Find(item => item.Name == email);
            if (value.Name != null)
            {
                return true;
            }
            return false;
        }
        public static SComProfile getProfile(int id)
        {
            var value = users.Find(item => item.currentId == id);
            if (value.Name != null)
            {
                return value;
            }
            return new SComProfile();
        }
        public static SComProfile getProfileByFile(string file)
        {
            var value = users.Find(item => item.ProfileFile == file);
            if (value.Name != null)
            {
                return value;
            }
            return new SComProfile();
        }
        public static int getProfileIdByFile(string file)
        {
            if (String.IsNullOrEmpty(file)) { return 0; }
            var value = users.Find(item => item.ProfileFile == file);
            if (value.Name != null)
            {
                return value.currentId;
            }

            return 0;
        }
        public static string getProfileFile(int id)
        {
            var value = users.Find(item => item.currentId == id);
            if (value.Name != null)
            {
                return value.ProfileFile;
            }
            return "";
        }
        public static bool hasAuthProfile()
        {
            if (!String.IsNullOrEmpty(authProfile.Email) && !String.IsNullOrEmpty(authProfile.Password))
            {
                if (!String.IsNullOrWhiteSpace(authProfile.Email) && !String.IsNullOrWhiteSpace(authProfile.Password))
                {
                    return true;
                }
            }

            return false;
        }
        public static void setAuthProfile(int id)
        {
            authProfile = getProfile(id);
        }
        public static string getAuthProfileFileName()
        {
            return String.IsNullOrEmpty(authProfile.ProfileFile) ? String.Empty : authProfile.ProfileFile;
        }
        private static void _gotXMLError(CXmlException e)
        {
            _hasXmlError = true;
        }
        public static bool canSendNow()
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

        #region HTTPCalls
        public static void sendScoreAsync(Action<SComResponse> callback)
        {
            var task = Task<SComResponse>.Factory.StartNew(() => _sendScoreAsync(callback));
        }
        public static void getScoresAsync(SComQueryHighScores data, Action<SComResultScore> callback)
        {
            var task = Task<SComResultScore>.Factory.StartNew(() => _getScoresAsync(data, callback));
        }
        public static void getContestsAsync(Action<SComResultContestList> callback)
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
        }
        #endregion

        private static SComResponse _sendScoreAsync(Action<SComResponse> callback)
        {
            currentScores.method = "setscores";
            currentScores.version = version;

            SComResponse result = new SComResponse();
            SComResponse response = Request(JsonConvert.SerializeObject(currentScores));
            if (response.status == 1)
            {
                try
                {
                    SComResponse jresult = JsonConvert.DeserializeObject<SComResponse>(response.result);
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    CBase.Log.LogError("CommunityClient: JSON Parse error: " + e.Message);
                    SComResponse jresult = new SComResponse();
                    jresult.message = e.Message;
                    jresult.status = 0;
                    jresult.code = -1;
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                result.code = response.code;
                if (callback != null) { callback(result); }
                return result;
            }
        }
        private static SComResultScore _getScoresAsync(SComQueryHighScores data, Action<SComResultScore> callback)
        {
            data.version = version;
            if (String.IsNullOrEmpty(data.method))
            {
                data.method = "getscores";
            }
            SComResultScore result = new SComResultScore();
            SComResponse response = Request(JsonConvert.SerializeObject(data));
            if (response.status == 1)
            {
                try
                {
                    SComResultScore jresult = JsonConvert.DeserializeObject<SComResultScore>(response.result);
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    CBase.Log.LogError("CommunityClient: JSON Parse error: " + e.Message);
                    CBase.Log.LogError("CommunityClient: Original response: " + response.result);
                    SComResultScore jresult = new SComResultScore();
                    jresult.message = e.Message;
                    jresult.status = 0;
                    jresult.code = -1;
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                result.code = response.code;
                if (callback != null) { callback(result); }
                return result;
            }
        }
        private static SComResultContestList _getContestsAsync(Action<SComResultContestList> callback)
        {
            SComQueryCmd query = new SComQueryCmd();
            query.method = "getcontests";
            query.version = version;

            if (hasAuthProfile())
            {
                query.username = authProfile.Email;
                query.password = authProfile.Password;
            }

            SComResultContestList result = new SComResultContestList();
            SComResponse response = Request(JsonConvert.SerializeObject(query));
            if (response.status == 1)
            {
                try
                {
                    SComResultContestList jresult = JsonConvert.DeserializeObject<SComResultContestList>(response.result);
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    CBase.Log.LogError("CommunityClient: JSON Parse error: " + e.Message);
                    CBase.Log.LogError("CommunityClient: Original response: " + response.result);
                    result.message = e.Message;
                    result.status = 0;
                    result.code = -1;
                    if (callback != null) { callback(result); }
                    return result;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                result.code = response.code;
                if (callback != null) { callback(result); }
                return result;
            }
        }
        private static SComResultCPlaylist _getCPlaylistAsync(int contestId, Action<SComResultCPlaylist> callback)
        {
            SComQueryCmd query = new SComQueryCmd();
            query.method = "getcontestplaylist";
            query.id = contestId;
            query.version = version;

            if (hasAuthProfile())
            {
                query.username = authProfile.Email;
                query.password = authProfile.Password;
            }

            SComResultCPlaylist result = new SComResultCPlaylist();
            SComResponse response = Request(JsonConvert.SerializeObject(query));
            if (response.status == 1)
            {
                try
                {
                    SComResultCPlaylist jresult = JsonConvert.DeserializeObject<SComResultCPlaylist>(response.result);
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    CBase.Log.LogError("NetStat: JSON Parse error: " + e.Message);
                    CBase.Log.LogError("NetStat: Original response: " + response.result);
                    result.message = e.Message;
                    result.status = 0;
                    result.code = -1;
                    if (callback != null) { callback(result); }
                    return result;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                result.code = response.code;
                if (callback != null) { callback(result); }
                return result;
            }
        }
        private static SComResponse _getContestAccessAsync(string[] profileFiles, int contestId, Action<SComResponse> callback)
        {
            SComQueryContestAccess query = new SComQueryContestAccess();
            query.method = "getcontestaccess";
            query.id = contestId;
            query.players = new SComQueryPlayers[profileFiles.Length];
            query.version = version;

            for (int i = 0; i < profileFiles.Length; i++)
            {
                var profile = getProfileByFile(profileFiles[i]);
                query.players[i] = new SComQueryPlayers();
                query.players[i].username = profile.Email;
                query.players[i].password = profile.Password;
            }

            SComResponse result = new SComResponse();
            SComResponse response = Request(JsonConvert.SerializeObject(query));
            if (response.status == 1)
            {
                try
                {
                    SComResponse jresult = JsonConvert.DeserializeObject<SComResponse>(response.result);
                    if (callback != null) { callback(jresult); }
                    return jresult;
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    CBase.Log.LogError("CommunityClient: JSON Parse error: " + e.Message);
                    CBase.Log.LogError("CommunityClient: Original response: " + response.result);
                    result.message = e.Message;
                    result.status = 0;
                    result.code = -1;
                    if (callback != null) { callback(result); }
                    return result;
                }
            }
            else
            {
                result.status = 0;
                result.message = response.message;
                result.code = response.code;
                if (callback != null) { callback(result); }
                return result;
            }
        }


        public static string _buildRequest(string method, EComQueryType queryType, Object customData = null)
        {
            if (queryType == EComQueryType.Cmd)
            {
                SComQueryCmd query = new SComQueryCmd();
                query.method = method;
                query.version = version;

                if (customData != null)
                {
                    query.queryobj = customData;
                    //query.querydata = Base64Encode(JsonConvert.SerializeObject(customData));
                }
                return JsonConvert.SerializeObject(query);
            }
            else if (queryType == EComQueryType.SendScore)
            {
                SComQueryScore query = new SComQueryScore();
                query.method = method;
                return JsonConvert.SerializeObject(query);
            }
            return String.Empty;
        }

        public static Object _buildResponse(string response, EComResultType resultType)
        {
            try
            {
                if (resultType == EComResultType.CmdStatus)
                {
                    Object jresult = JsonConvert.DeserializeObject<Object>(response);
                    return jresult;
                }
                else if (resultType == EComResultType.HighScores)
                {
                    Object jresult = JsonConvert.DeserializeObject<Object>(response);
                    return jresult;
                }
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                //Console.Write("Original response: " + response);
                CBase.Log.LogError("CommunityClient: JSON Parse error: " + e.Message);
                SComResponse jresult = new SComResponse();
                jresult.message = e.Message;
                jresult.status = 0;
                jresult.code = -1;
                return jresult;
            }

            SComResponse r = new SComResponse();
            r.message = "Internal error";
            r.status = 0;
            r.code = -1;
            return r;
        }
        public static SComResponse Request(string requestData)
        {
            SComResponse result = new SComResponse();
            if (isEnabled() != true)
            {
                result.status = 0;
                result.code = -1;
                result.message = "NetStat is not enabled.";
                return result;
            }

            var request = (HttpWebRequest)WebRequest.Create(config.Server);
            request.Timeout = 10000;
            request.ContentType = "application/json";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "POST";

            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(requestData);
                    streamWriter.Flush();
                    streamWriter.Close();
                    var httpResponse = (HttpWebResponse)request.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result.status = 1;
                        result.result = streamReader.ReadToEnd();

                        return result;
                    }
                }
            }
            catch (WebException e)
            {
                result.status = 0;
                result.code = -1;
                result.message = e.Message.ToString();
                return result;
            }
            catch (Exception e)
            {
                result.status = 0;
                result.code = -1;
                result.message = e.Message.ToString();
                return result;
            }
        }

        private static Action<SComDownloadStatus> _dlProgressCallback;
        private static Action<SComDownloadStatus> _dlDoneCallback;

        public static void downloadFile(string url, string path, Action<SComDownloadStatus> progressCallback, Action<SComDownloadStatus> doneCallback)
        {
            _dlProgressCallback = progressCallback;
            _dlDoneCallback = doneCallback;
            WebClient Client = new WebClient();
            Client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_dlProgress);
            Client.DownloadFileCompleted += new AsyncCompletedEventHandler(_dlDone);
            Client.DownloadFileAsync(new Uri(url), path);


        }

        private static void _dlDone(object sender, AsyncCompletedEventArgs e)
        {
            SComDownloadStatus status = new SComDownloadStatus();
            status.Done = true;
            if (e.Error != null)
            {
                status.Error = 1;
            }
            else
            {
               /* if (e.Cancelled != null)
                {
                    status.Error = 2;
                }
                else
                {
                    status.Error = 0;
                }*/
            }
            if (_dlDoneCallback != null)
                _dlDoneCallback(status);
        }

        private static void _dlProgress(object sender, DownloadProgressChangedEventArgs e)
        {

            SComDownloadStatus status = new SComDownloadStatus();
            status.BytesReceived = e.BytesReceived;
            status.TotalBytes = e.TotalBytesToReceive;
            status.Percentage = e.BytesReceived / e.TotalBytesToReceive * 100;

            if (_dlProgressCallback != null)
                _dlProgressCallback(status);
        }

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
    }

    #region Structs
    public struct SComDownloadStatus
    {
        public double BytesReceived;
        public double TotalBytes;
        public double Percentage;
        public int Error;
        public bool Done;
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

    public struct SComQueryCmd
    {
        public string version;
        public string method;
        public string querydata;
        public int id;
        public Object queryobj;
        public string username;
        public string password;
    }

    public struct SComQueryContestAccess
    {
        public string method;
        public string version;
        public int id;
        public SComQueryPlayers[] players;
    }

    public struct SComQueryPlayers
    {
        public string username;
        public string password;
    }

    public struct SComQueryHighScores
    {
        public string version;
        public string username;
        public string password;
        public string method;
        public int difficulty;
        public int id;
        public string txthash;
        public string gameMode;
        public string partyMode;
        public string queryType;
    }

    public struct SComQueryScore
    {
        public string version;
        public string method;
        public string gameMode;
        public string partyMode;
        public int guests;
        public SComQueryScoreItem[] scores;
    }

    public class SComQueryScoreItem
    {
        public string username;
        public string password;
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

    public struct SComResponse
    {
        public int status;
        public int code;
        public string message;
        public string result;
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

    public struct SComProfile
    {
        [XmlElement("Name")]
        public string Name;
        [XmlElement("Email")]
        public string Email;
        [XmlElement("Password")]
        public string Password;
        public string ProfileFile;
        public int currentId;
    }

    #endregion

}
