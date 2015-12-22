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
using System.Linq;
using System.Text;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Community
{
    public static class CCommunitySong
    {
        private struct SComSongDownload
        {
            public string url;
            public string name;
            public string folder;
            public string type;
            public string filename;
        }
        private static SPopupGeneralProgress _Progress1;
        private static SPopupGeneralProgress _Progress2;



        public static int FindSongByHash(string hash)
        {
            return CBase.Songs.FindSongIdByHash(hash);
        }

        public static int FindSongByHash(string[] hashes)
        {
            int songId = -1;
            foreach (String hash in hashes) {
                songId = CBase.Songs.FindSongIdByHash(hash);
                if (songId > -1) { return songId; }
            }
            return songId;
        }

        public static string GetSongFolder()
        {
            var folders = CBase.Config.GetSongFolders();
            if (folders.Count() == 0)
                return String.Empty;

            if (!System.IO.Directory.Exists(System.IO.Path.Combine(folders.ElementAt(0), "Community"))) {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(folders.ElementAt(0), "Community"));
            }
            return System.IO.Path.Combine(folders.ElementAt(0), "Community");
        }

        public static string CreateSongFolder(string artist, string title, bool unique = false)
        {
            var basepath = GetSongFolder();
            if (String.IsNullOrEmpty(basepath))
                return String.Empty;

            var artistfolder = System.IO.Path.Combine(basepath, CHelper.GetFilenameFromString(artist));
            if (!System.IO.Directory.Exists(artistfolder))
            {
                System.IO.Directory.CreateDirectory(artistfolder);
            }

            var i = 1;
            var songfolder = System.IO.Path.Combine(artistfolder, CHelper.GetFilenameFromString(title));
            if (unique == true) { 
                while (System.IO.Directory.Exists(songfolder))
                {
                    songfolder = System.IO.Path.Combine(artistfolder, CHelper.GetFilenameFromString(title)+"("+i+")");
                    i++;
                }
            }
            else
            {
                if (!System.IO.Directory.Exists(songfolder))
                {
                    System.IO.Directory.CreateDirectory(songfolder);
                }
            }

            return songfolder;
        }
       
        public static void AddToDownloads(string name, string url, string path, string type = null)
        {
            var item = new SComSongDownload();
            item.name = name;
            item.url = url;
            item.folder = path;
            item.type = type;
            _filesToDownload.Add(item);
        }


        public static void DownloadSong(SComRemoteSong song, Action<bool> callback = null, bool silent = false)
        {
            string songFolder = null;

            if (song.fileList != null) {
                if (song.fileList["txt"] != null && song.fileList["audio"] != null)
                {
                    songFolder = CreateSongFolder(song.artist, song.title);
                    if (String.IsNullOrEmpty(songFolder))
                    {
                        if (!silent) { CPopupHelper.Alert("Error!", "Song folder not found!"); }
                        if (callback != null)
                            callback(false);

                        return;
                    }


                    //valid keys: imgcover, audio, video, zip, txt, imgbg[0-9]
                    foreach (KeyValuePair<string, Dictionary<string,string>> file in song.fileList)
                    {
                        if (file.Value != null && file.Value.Count() > 0) {
                            if (file.Value["url"] != null && file.Value["ext"] != null) { 
                                var dl = new SComSongDownload();
                                dl.name     = song.artist + " - " + song.title + "(" + file.Value["ext"] + ")";
                                dl.url      = file.Value["url"];
                                dl.type     = file.Key.IndexOf("img")!=-1?"image":file.Key;
                                dl.folder   = songFolder;
                                dl.filename = file.Value["fn"] != null ? file.Value["fn"] : "songdata_"+file.Key+"." + file.Value["ext"];
                                _filesToDownload.Add(dl);
                            }
                        }
                    }
                }
                else if (song.fileList["zip"] != null)
                {
                    songFolder = CreateSongFolder(song.artist, song.title);
                }

                if (_filesToDownload.Count > 0)
                {
                    
                    _Progress1 = new SPopupGeneralProgress();
                    _Progress2 = new SPopupGeneralProgress();

                    if (!silent) {
                        _CreateProgressPopup();
                    }

                    _DownloadProgressCallback = delegate(SComDownloadStatus progress)
                    {
                        if (!silent)
                        {
                            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
                            _Progress1.Target = 1;
                            _Progress1.Title = (_DownloadCurrentIndex) + " of " + _filesToDownload.Count + " files";
                            _Progress1.Loaded = _DownloadCurrentIndex;
                            _Progress1.Total = _filesToDownload.Count;
                            popup.SetProgressData(_Progress1);

                            _Progress2.Target = 2;
                            _Progress2.Title = _DownloadCurrent.name;
                            _Progress2.Loaded = (float)progress.BytesReceived;
                            _Progress2.Total = (float)progress.TotalBytes;
                            popup.SetProgressData(_Progress2);
                        }
                    };

                    _DownloadDoneCallback = delegate(SComDownloadStatus status)
                    {

                        if (status.Error==0)
                        {
                            var contentType = status.HttpHeaders.FirstOrDefault(x => x.Key == "Content-Type").Value;
                            if (contentType == null)
                            {
                                _DownloadErrors++;
                            }
                            else
                            {
                                if (_DownloadCurrent.type == "audio" && contentType.IndexOf("audio/") == -1)
                                {
                                    _DownloadErrors++;
                                }
                                else if (_DownloadCurrent.type == "image" && contentType.IndexOf("image/") == -1)
                                {
                                    _DownloadErrors++;
                                }
                                else if (_DownloadCurrent.type == "video" && contentType.IndexOf("video/") == -1)
                                {
                                    _DownloadErrors++;
                                }
                                else if (_DownloadCurrent.type == "txt" && contentType.IndexOf("plain/text") == -1)
                                {
                                    _DownloadErrors++;
                                }
                                else if (_DownloadCurrent.type == "zip" && contentType.IndexOf("application/octet-stream") == -1 || _DownloadCurrent.type == "zip" && contentType.IndexOf("application/zip") == -1 )
                                {
                                    _DownloadErrors++;
                                }
                            }
                        }

                        if (_DownloadCurrentIndex+1 == _filesToDownload.Count)
                        {
                            if (_DownloadErrors == 0) {
                                //find txt file in new folder
                                string[] txtfiles = System.IO.Directory.GetFiles(songFolder, "*.txt");
                                if (txtfiles.Length >= 1) {
                                    if (CBase.Songs.AddNewSong(txtfiles[0]) != false)
                                    {
                                        if (callback != null)
                                            callback(true);

                                        if (!silent)
                                        {
                                            CPopupHelper.Alert("Download", "Complete!");
                                        }
                                        
                                        return;
                                    }
                                }
                            }
                    
                            if (!silent)
                            {
                                CPopupHelper.Alert("Download", "Not completed. Some file is missing.");
                            }

                            if (callback != null)
                                callback(false);
                            
                        }
                        else
                        {
                            if (!silent) {
                                var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);

                                _Progress1.Target = 1;
                                _Progress1.Title = (_DownloadCurrentIndex + 1) + " of " + _filesToDownload.Count + " files";
                                _Progress1.Loaded = _DownloadCurrentIndex + 1;
                                _Progress1.Total = _filesToDownload.Count;
                                popup.SetProgressData(_Progress1);

                                
                            }
                        }
                        
                    };
                    //start process
                    _Downloader();
                }
                else
                {
                    if (callback != null)
                        callback(false);

                    if (!silent)
                    {
                        CPopupHelper.Alert("Download error", "Missing required file(s)...");
                    }
                }
            }
        }

        public static void UpdateSongTextFile(string txtFileUrl, int songId, Action<bool> callback = null, bool silent = false)
        {
            var song = CBase.Songs.GetSongByID(songId);
            var updateFile = System.IO.Path.Combine(song.Folder, "update" + songId + ".tmp");
            if (System.IO.File.Exists(updateFile))
            {
                System.IO.File.Delete(updateFile);
            }

            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);

            if (!silent)
            {
                CPopupHelper.Download("Downloading updates ...", song.Artist.ToString() + " - " + song.Title.ToString());
            }

            CCommunity.downloadFileAsync(txtFileUrl, updateFile, delegate(SComDownloadStatus progress)
            {
                if (!silent)
                {
                    var prog = new SPopupGeneralProgress();
                    prog.Percentage = (float)progress.Percentage;
                    popup.SetProgressData(prog);
                }
            }, delegate(SComDownloadStatus status)
            {
                if (status.Error > 0)
                {
                    CPopupHelper.Alert("Update error", "Cant download TXT file!");
                    if (callback != null)
                        callback(false);
                }
                else
                {
                    var contentType = status.HttpHeaders.FirstOrDefault(x => x.Key == "Content-Type").Value;
                    if (contentType == null || contentType != "plain/text")
                    {
                        if (System.IO.File.Exists(updateFile))
                        {
                            System.IO.File.Delete(updateFile);
                        }
                        if (!silent)
                        {
                            CPopupHelper.Alert("Update error", "Cant download TXT file!");
                        }
                        if (callback != null)
                            callback(false);
                    }
                    else
                    {
                        if (!silent)
                        {
                            var prog = new SPopupGeneralProgress();
                            prog.Percentage = 100;
                            popup.SetProgressData(prog);
                        }

                        var newSong = CBase.Songs.CheckSongFile(updateFile);
                        if (newSong == null)
                        {
                            CPopupHelper.Alert("Update error", "Cant load update file!");
                            if (callback != null)
                                callback(false);
                        }
                        else
                        {
                            //check cover 
                            if (!String.IsNullOrWhiteSpace(newSong.CoverFileName) && newSong.CoverFileName != song.CoverFileName)
                            {
                                if (System.IO.File.Exists(System.IO.Path.Combine(song.Folder, song.CoverFileName)))
                                    System.IO.File.Move(System.IO.Path.Combine(song.Folder, song.CoverFileName), System.IO.Path.Combine(song.Folder, newSong.CoverFileName));
                            }
                            //check mp3 file
                            if (newSong.MP3FileName != song.MP3FileName)
                            {
                                if (System.IO.File.Exists(System.IO.Path.Combine(song.Folder, song.MP3FileName)))
                                    System.IO.File.Move(System.IO.Path.Combine(song.Folder, song.MP3FileName), System.IO.Path.Combine(song.Folder, newSong.MP3FileName));
                            }
                            //check background files
                            if (song.BackgroundFileNames.Count > 0 && newSong.BackgroundFileNames.Count > 0)
                            {
                                for (int i = 0; i < song.BackgroundFileNames.Count; i++ )
                                {
                                    if (i < newSong.BackgroundFileNames.Count && newSong.BackgroundFileNames[i] != null && song.BackgroundFileNames[i] != newSong.BackgroundFileNames[i])
                                    {
                                        if (System.IO.File.Exists(System.IO.Path.Combine(song.Folder, song.BackgroundFileNames[i])))
                                            System.IO.File.Move(System.IO.Path.Combine(song.Folder, song.BackgroundFileNames[i]), System.IO.Path.Combine(song.Folder, newSong.BackgroundFileNames[i]));
                                    }
                                }
                            }
                            //check video
                            if (!String.IsNullOrWhiteSpace(newSong.VideoFileName))
                            {
                                if (!String.IsNullOrWhiteSpace(song.VideoFileName))
                                {
                                    if (newSong.VideoFileName != song.VideoFileName)
                                    {
                                        if (System.IO.File.Exists(System.IO.Path.Combine(song.Folder, song.VideoFileName)))
                                            System.IO.File.Move(song.VideoFileName, System.IO.Path.Combine(song.Folder, newSong.VideoFileName));
                                    }
                                }   
                            }
                            //rename files
                            var originalTextFile = System.IO.Path.Combine(song.Folder, song.FileName);
                            System.IO.File.Move(originalTextFile, System.IO.Path.Combine(song.Folder, CHelper.GetUniqueFileName(song.Folder, song.FileName + ".old")));
                            System.IO.File.Move(updateFile, System.IO.Path.Combine(song.Folder, song.FileName));

                            if (CBase.Songs.ReloadSong(songId) == false)
                            {
                                if (!silent)
                                {
                                    CPopupHelper.Alert("Update error", "Cant reload song!");
                                    if (callback != null)
                                        callback(false);
                                }
                            }
                            else
                            {
                                if (!silent)
                                {
                                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                                }
                                if (callback != null)
                                    callback(true);
                            }
                        }
                    }
                }
            });
        }

        private static void _CreateProgressPopup()
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyEscape", (Action<SPopupGeneralEvent>)_CancelDownloadWindow);
            data.Type = EPopupGeneralType.Loading;
            data.Size = EPopupGeneralSize.Medium;
            data.ButtonOkLabel = "Cancel";
            data.TextTitle = "Downloading...";
            data.ProgressBar1Title = _DownloadCurrentIndex + " of " + _filesToDownload.Count + " files";
            data.ProgressBar1Visible = true;
            data.ProgressBar2Title = "";
            data.ProgressBar2Visible = true;
            popup.SetDisplayData(data);

            if (_Progress1.Target > 0)
                popup.SetProgressData(_Progress1);
            if (_Progress2.Target > 0)
                popup.SetProgressData(_Progress2);

            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private static void _CancelDownloadWindow(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyEscape"))
            {
                var wndopts = new SPopupGeneral();
                wndopts.TextTitle = "chgd";
                wndopts.Size = EPopupGeneralSize.Small;
                CPopupHelper.Confirm("Confirmation", "Cancel download process?", (Action<SPopupGeneralEvent>)_ConfirmCancelDownload, wndopts);
            }
        }

        private static void _ConfirmCancelDownload(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyEscape")
                || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
            )
            {
                _CreateProgressPopup();
            }
            else if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
            )
            {
                _ResetDownloader();
                CCommunity.cancelDownloadAsync();
                CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            }
        }

        private static List<SComSongDownload> _filesToDownload = new List<SComSongDownload>();
        private static int _DownloadCurrentIndex = 0;
        private static int _DownloadErrors = 0;
        private static SComSongDownload _DownloadCurrent;
        private static Action<SComDownloadStatus> _DownloadProgressCallback;
        private static Action<SComDownloadStatus> _DownloadDoneCallback;

        private static void _ResetDownloader()
        {
            _filesToDownload = new List<SComSongDownload>();
            _DownloadCurrentIndex = 0;
            _DownloadCurrent = new SComSongDownload();
            _DownloadProgressCallback = null;
            _DownloadDoneCallback = null;
            _DownloadErrors = 0;
        }

        private static Boolean _Downloader()
        {
            if (_filesToDownload.Count > 0)
            {
                if (_DownloadCurrentIndex == _filesToDownload.Count)
                {
                    _ResetDownloader();
                    return true;
                }
                _DownloadCurrent = _filesToDownload[_DownloadCurrentIndex];
                CCommunity.downloadFileAsync(_DownloadCurrent.url, System.IO.Path.Combine(_DownloadCurrent.folder, _DownloadCurrent.filename), delegate(SComDownloadStatus progress)
                {
                    if (_DownloadProgressCallback != null)
                        _DownloadProgressCallback(progress);

                }, delegate(SComDownloadStatus status)
                {
                    if (status.Error > 0)
                    {
                        _DownloadErrors++;
                    }

                    if (_DownloadDoneCallback != null)
                        _DownloadDoneCallback(status);

                    if (_DownloadCurrentIndex < _filesToDownload.Count)
                    {
                        _DownloadCurrentIndex++;
                        _Downloader();
                    }
                    else
                    {
                        _ResetDownloader();
                    }
                });
                return true;
            }
            return false;
        }
    }
}
