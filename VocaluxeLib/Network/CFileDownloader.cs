#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion



using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;

namespace VocaluxeLib.Network
{
    public enum CFileDlType
    {
        Unknown = -1,
        StandardUrl = 0,
        Youtube = 1,
        Mega = 2
    }

    public enum CFileDlFileType
    {
        Video = 1,
        Audio = 2
    }

    public enum CFileDlEventStatus
    {
        Wait = -1,
        Progress = 0,
        Success = 1,
        Error = 2
    }

    public struct CFileDlYtFormats
    {
        public string videoUrl;
        public string format;
        public string quality;
        public string sig;
        public string itag;
    }

    public struct CFileDlFile
    {
        public string url;
        public string fileName;
        public bool extractAudio;
        public string fileType;
        public string audioFormat;
        public string extension;
        public string path;
    }

    public struct CFileDlEvent
    {
        public CFileDlEventStatus status;
        public double BytesTotal;
        public double BytesReceived;
        public int BytesPercent;
        public CFileDlFile target;
        public int FilesCount;
        public int CurrentFile;
        public string message;
    }

    public class CFileDLWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            //w.Timeout = 10000;
            w.Method = "GET";
            return w;
        }
    }

    public class CFileDownloader
    {
        public string videoID = String.Empty;
        public CFileDlType type = CFileDlType.Unknown;
        public string    YoutubePreferredQuality = "medium";
        public bool      YoutubeDownloadVideo = true;
        public List<CFileDlFile> FileList = new List<CFileDlFile>();
        public string DownloadUrl;

        private Action<CFileDlEvent> _DownloadProgressCallback;
        private CFileDlFile _DownloadCurrent;
        private int _DownloadCurrentIndex = -1;


        public void init()
        {
            FileList = new List<CFileDlFile>();
            FileList.Resize(0);
            _DownloadCurrentIndex = -1;
            videoID = String.Empty;
        }

        public bool parseUrl(string theurl)
        {
            Uri url;
            try { 
                 url = new Uri(theurl);
            }
            catch (Exception e)
            {
                return false;
            }

            if (Regex.IsMatch(url.Host, "youtube.com", RegexOptions.IgnoreCase) || Regex.IsMatch(url.Host, "youtu.be", RegexOptions.IgnoreCase))
            {
                type = CFileDlType.Youtube;

                if (Regex.IsMatch(url.Host, "youtu.be", RegexOptions.IgnoreCase))
                {
                    if (url.Segments.Length >= 2)
                    {
                        videoID = url.Segments[1];
                    }
                }
                else
                {
                    if (url.Segments.Length >= 2)
                    {
                        if (url.Segments[1].ToString().IndexOf("embed")>-1)
                        {
                            if (url.Segments.Length >= 3)
                            {
                                videoID = url.Segments[2];
                            }
                        }
                    }
                }
                if (String.IsNullOrEmpty(videoID))
                {
                    if (Regex.IsMatch(url.Query, "v=", RegexOptions.IgnoreCase)) {
                        var qvars = parseQuery(url.Query);
                        var vid = qvars.Find(item => item.Key == "v");
                        if (!String.IsNullOrEmpty(vid.Value) && !String.IsNullOrWhiteSpace(vid.Value))
                        {
                            videoID = vid.Value;
                        }
                    }
                }
                else
                {
                    if (videoID.Substring(videoID.Length-1, 1) == "/")
                    {
                        videoID = videoID.Substring(0, videoID.Length - 1);
                    }
                }

                if (String.IsNullOrEmpty(videoID))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if ( Regex.IsMatch(url.Host, "mega.co.nz", RegexOptions.IgnoreCase) ) {
                type = CFileDlType.Mega;
                DownloadUrl = theurl;
                return true;
            }
            else if (url.Host != null)
            {
                type = CFileDlType.StandardUrl;
                DownloadUrl = theurl;
                return true;
            }

            return false;
        }

        public void prepareDownload(string path, string fileNamePrefix, Action<Boolean> status)
        {
            if (type == CFileDlType.Youtube)
            {
                if (!String.IsNullOrEmpty(videoID))
                {
                    prepareDownloadYoutube(path, fileNamePrefix, status);
                    return;
                }
            }
            else if (type == CFileDlType.StandardUrl)
            {
                prepareDownloadStandard(path, fileNamePrefix, status);
                return;
            }
            status(false);
        }

        private void prepareDownloadStandard(string path, string fileNamePrefix, Action<Boolean> status)
        {
            var request = (HttpWebRequest)WebRequest.Create(DownloadUrl);
            request.Timeout = 10000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "HEAD";
            request.AllowAutoRedirect = true;

            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                if (httpResponse.Headers.Count > 0)
                {
                    string contentType = null;
                    string contentDisposition = null;

                    foreach (string header in httpResponse.Headers)
                    {
                        if (header.ToLower().Equals("content-type"))
                        {
                            contentType = httpResponse.Headers[header];
                        }
                        else if (header.ToLower().Equals("content-disposition"))
                        {
                            contentDisposition = httpResponse.Headers[header];
                        }
                    }


                    if (httpResponse.StatusCode == HttpStatusCode.OK && contentType != null)
                    {
                        CFileDlFile file = new CFileDlFile();
                        file.path = path;
                        file.url = DownloadUrl;
                        if (Regex.IsMatch(contentType, "zip", RegexOptions.IgnoreCase))
                        {
                            file.extension = "zip";
                            file.fileName = fileNamePrefix + ".zip";
                            file.fileType = "zip";
                            file.extractAudio = false;
                            FileList.Add(file);
                            status(true);
                            httpResponse.Close();
                            return;
                        }
                        else if (Regex.IsMatch(contentType, "(audio|mp3)", RegexOptions.IgnoreCase))
                        {
                            file.extension = "mp3";
                            file.fileName = fileNamePrefix + ".mp3";
                            file.fileType = "audio";
                            file.extractAudio = false;
                            FileList.Add(file);
                            status(true);
                            httpResponse.Close();
                            return;
                        }
                        else if (Regex.IsMatch(contentType, "video", RegexOptions.IgnoreCase))
                        {
                            file.extension = "mp4";
                            file.fileName = fileNamePrefix + ".mp4";
                            file.fileType = "video";
                            file.extractAudio = false;
                            FileList.Add(file);
                            status(true);
                            return;
                        }
                        else if (Regex.IsMatch(contentType, "image", RegexOptions.IgnoreCase))
                        {
                            file.extension = "jpg";
                            file.fileName = fileNamePrefix + ".jpg";
                            file.fileType = "image";
                            file.extractAudio = false;
                            FileList.Add(file);
                            status(true);
                            httpResponse.Close();
                            return;
                        }
                        else if (Regex.IsMatch(contentType, "text", RegexOptions.IgnoreCase))
                        {
                            file.extension = "txt";
                            file.fileName = fileNamePrefix + ".txt";
                            file.fileType = "txt";
                            file.extractAudio = false;
                            FileList.Add(file);
                            status(true);
                            httpResponse.Close();
                            return;
                        }
                        else if (Regex.IsMatch(contentType, "application/octet-stream", RegexOptions.IgnoreCase))
                        {
                            //try to determine by extension
                            if (contentDisposition != null)
                            {
                                if (Regex.IsMatch(contentDisposition, "filename=", RegexOptions.IgnoreCase))
                                {
                                    if (Regex.IsMatch(contentDisposition, "\\.(zip|bz|gz)", RegexOptions.IgnoreCase))
                                    {
                                        file.extension = "zip";
                                        file.fileName = fileNamePrefix + ".zip";
                                        file.fileType = "zip";
                                        file.extractAudio = false;
                                        FileList.Add(file);
                                        status(true);
                                        httpResponse.Close();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("DLError:" + e.Message);
                status(false);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("DLError:" + e.Message);
                status(false);
                return;
            }
            status(false);
            return;
        }

        private void prepareDownloadYoutube(string path, string fileNamePrefix, Action<Boolean> status)
        {
            List<CFileDlYtFormats> videoFormats = new List<CFileDlYtFormats>();

            string ytinfourl = "http://www.youtube.com/get_video_info?&video_id=" + videoID + "&asv=3&el=detailpage&hl=en_US";
            var request = (HttpWebRequest)WebRequest.Create(ytinfourl);
            request.Timeout = 10000;
            request.Referer = "http://www.youtube.com/v/"+videoID+"?version=3&f=videos&app=youtube_gdata";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Method = "GET";
            request.AllowAutoRedirect = true;

            try
            {
                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    string response = streamReader.ReadToEnd();
                    streamReader.Close();
                    var qvars = parseQuery(response);

                    var vf = qvars.Find(item => item.Key == "url_encoded_fmt_stream_map");
                    if (!String.IsNullOrEmpty(vf.Value) && !String.IsNullOrWhiteSpace(vf.Value))
                    {
                       // string formats = Uri.UnescapeDataString(videoformats.Value);
                        string[] vtmp = vf.Value.Split(new string[] { "," }, StringSplitOptions.None);
                        for (int i = 0; i < vtmp.Length; i++)
                        {
                            var kvs = parseQuery(vtmp[i]);
                            for (int z = 0; z < kvs.Count; z++)
                            {
                             //   Console.WriteLine("Key:" + kvs[z].Key + " => " + Uri.UnescapeDataString(kvs[z].Value));
                            }

                            CFileDlYtFormats pformat = new CFileDlYtFormats();
                            var itag = kvs.Find(item => item.Key == "itag");
                            pformat.itag = itag.Value;
                            var quality = kvs.Find(item => item.Key == "quality");
                            pformat.quality = quality.Value;
                            var sig = kvs.Find(item => item.Key == "sig");
                            pformat.sig = sig.Value;
                            var url = kvs.Find(item => item.Key == "url");
                            pformat.videoUrl = Uri.UnescapeDataString(url.Value);
                            var format = kvs.Find(item => item.Key == "type");
                            pformat.format = format.Value;

                            if (stringNotNull(url.Value))
                            {
                                videoFormats.Add(pformat);
                            }
                        }
                    }
                }
                httpResponse.Close();

                CFileDlFile audioFile = new CFileDlFile();
                CFileDlFile videoFile = new CFileDlFile();
                CFileDlFile PossibleVideoFile = new CFileDlFile();

                //search for acceptable formats
                if (videoFormats.Count > 0)
                {
                    for (int i = 0; i < videoFormats.Count; i++)
                    {
                        Console.WriteLine(videoFormats[i].format + " : " + videoFormats[i].itag);
                        if (videoFormats[i].format.IndexOf("video/x-flv") > -1) {
                            if (videoFormats[i].itag == "5" || videoFormats[i].itag == "18") 
                                if (audioFile.url == null)
                                {
                                    audioFile.url = videoFormats[i].videoUrl;
                                    audioFile.extractAudio = true;
                                    audioFile.fileType = "video/x-flv";
                                    audioFile.fileName = fileNamePrefix + ".flv";
                                    audioFile.path = path;
                                    audioFile.extension = "flv";
                                
                                }
                        }
                        else if (YoutubeDownloadVideo && videoFormats[i].format.IndexOf("video/mp4") > -1)
                        {
                            if (videoFormats[i].quality.IndexOf(YoutubePreferredQuality) > -1)
                            {
                                if (videoFile.url == null)
                                {
                                    videoFile.url = videoFormats[i].videoUrl;
                                    videoFile.extractAudio = false;
                                    videoFile.fileType = "video/mp4";
                                    videoFile.fileName = fileNamePrefix + ".mp4";
                                    videoFile.extension = "mp4";
                                    videoFile.path = path;
                                }
                            }
                            else
                            {
                                if (PossibleVideoFile.url == null)
                                {
                                    PossibleVideoFile.url = videoFormats[i].videoUrl;
                                    PossibleVideoFile.extractAudio = false;
                                    PossibleVideoFile.fileType = "video/mp4";
                                    PossibleVideoFile.fileName = fileNamePrefix + ".mp4";
                                    PossibleVideoFile.extension = "mp4";
                                    PossibleVideoFile.path = "mp4";
                                }
                            }  
                        }
                    }
                    //cant download currently (@TODO: no audio/video conversion at this moment)
                    if (audioFile.url == null)
                    {
                        status(false);
                        return;
                    }
                    else
                    {
                        FileList.Add(audioFile);
                    }

                    if (videoFile.url == null)
                    {
                        if (PossibleVideoFile.url != null)
                        {
                            FileList.Add(PossibleVideoFile);
                        }
                    }
                    else
                    {
                        FileList.Add(videoFile);
                    }
                    status(true);
                    return;
                }
                else
                {
                    status(false);
                    return;
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("DLError:" + e.Message);
                status(false);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("DLError:" + e.Message);
                status(false);
                return;
            }
            status(false);
            return;
        }

        public void DownloadFiles(Action<CFileDlEvent> callback)
        {
            _DownloadProgressCallback = callback;
            _DownloadCurrentIndex = -1;
           // _Downloader();
            var task = Task<Boolean>.Factory.StartNew(() => _Downloader());
        }

        private Boolean _Downloader()
        {
            if (FileList.Count > 0) { 
                if (_DownloadCurrentIndex < FileList.Count)
                {
                    _DownloadCurrentIndex++;
                }
                
                _DownloadCurrent = FileList[_DownloadCurrentIndex];

                CFileDlEvent eventData = new CFileDlEvent();
                eventData.status = CFileDlEventStatus.Wait;
                eventData.CurrentFile = _DownloadCurrentIndex;
                eventData.FilesCount = FileList.Count;
                eventData.message = "Downloading from: " + _DownloadCurrent.url;
                if (_DownloadProgressCallback != null)
                {
                    _DownloadProgressCallback(eventData);
                }

                CFileDLWebClient client = new CFileDLWebClient();
                
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(_DownloadProgressComplete);
                try
                {
                    client.DownloadFileAsync(new Uri(_DownloadCurrent.url), Path.Combine(_DownloadCurrent.path, _DownloadCurrent.fileName));
                    //client.DownloadFile(new Uri(_DownloadCurrent.url), Path.Combine(_DownloadCurrent.path, _DownloadCurrent.fileName));
                }
                catch (WebException e)
                {
                    Console.WriteLine("WebException: " + e.Message);
                }
                return true;
            }

            Console.WriteLine("FileList empty? " + FileList.Count);

            CFileDlEvent evdata = new CFileDlEvent();
            evdata.status = CFileDlEventStatus.Error;
            evdata.message = "FileList is empty.";
            _DownloadProgressCallback(evdata);

            return false;
        }

        private void _DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
           // Console.Write("Progress changed ...");

            CFileDlEvent eventData = new CFileDlEvent();
            eventData.status = CFileDlEventStatus.Progress;
            eventData.target = _DownloadCurrent;
            eventData.CurrentFile = _DownloadCurrentIndex;
            eventData.FilesCount = FileList.Count;
            eventData.BytesReceived = double.Parse(e.BytesReceived.ToString());
            eventData.BytesTotal = double.Parse(e.TotalBytesToReceive.ToString());
            eventData.BytesPercent = int.Parse(Math.Truncate(eventData.BytesReceived / eventData.BytesTotal * 100).ToString());
            if (_DownloadProgressCallback != null)
            {
                _DownloadProgressCallback(eventData);
            }
        }

        private void _DownloadProgressComplete(object sender, AsyncCompletedEventArgs e)
        {
           // Console.Write("Completed ...");

            CFileDlEvent eventData = new CFileDlEvent();

            if (e.Error != null) {
                eventData.status = CFileDlEventStatus.Error;
                eventData.message = e.Error.ToString();
                Console.WriteLine("Error downloading {0}: {1}", e.UserState, e.Error);
            }
            else if (e.Cancelled)
            {
                eventData.status = CFileDlEventStatus.Error;
                eventData.message = e.Error.ToString();
                Console.WriteLine("Error downloading {0}: {1}", e.UserState, e.Error);
            }
            else
            {
                eventData.status = CFileDlEventStatus.Success;
            }
           
            eventData.target = _DownloadCurrent;
            eventData.CurrentFile = _DownloadCurrentIndex;
            eventData.FilesCount = FileList.Count;

            if (_DownloadProgressCallback != null)
            {
                _DownloadProgressCallback(eventData);
            }

            if (_NeedContinueDownload())
            {
                _Downloader();
            }

            //((IDisposable)sender).Dispose();
        }

        private bool _NeedContinueDownload()
        {
            if (_DownloadCurrentIndex+1 < FileList.Count)
            {
                return true;
            }
            return false;
        }

        private Boolean stringNotNull(string s)
        {
            return !String.IsNullOrEmpty(s) && !String.IsNullOrWhiteSpace(s);
        }

        public List<KeyValuePair<string, string>> parseQuery(String query)
        {
            List<KeyValuePair<string, string>> kvpairs = new List<KeyValuePair<string, string>>();

            if (query.Substring(0, 1) == "?")
            {
                query = query.Substring(1);
            }

            string[] kvtmp = new String[2];
            string[] qtmp = query.Split(new string[] { "&" }, StringSplitOptions.None);
            for (int i = 0; i < qtmp.Length; i++)
            {
                kvtmp = qtmp[i].Split(new string[] { "=" }, StringSplitOptions.None);
                if (!String.IsNullOrEmpty(kvtmp[0]) && !String.IsNullOrWhiteSpace(kvtmp[0])) { 
                    kvpairs.Add(new KeyValuePair<String, String>(kvtmp[0], Uri.UnescapeDataString(kvtmp[1])));
                }
            }

            return kvpairs;
        }
    }
}
