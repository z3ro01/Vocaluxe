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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Windows.Forms;
using VocaluxeLib.Menu;
using VocaluxeLib.Network;

namespace VocaluxeLib.PartyModes.CommunityContest
{
    public class CPartyScreenCommunityContestConfig : CPartyScreenCommunityContest
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const int _Lines = 5;
        private const string _TextTitleLeft = "TextTitleLeft";
        private const string _TextTitleCenter = "TextTitleCenter";
        private const string _TextTitleRight = "TextTitleRight";

        private string _Themex = "";

        private string[] _TextFields;

        private List<SComResultContestItem> _ContestList = new List<SComResultContestItem>();
        private int _ScrollPos = 0;
        private bool _NeedUpdate = false;
        private int[] _SelectableHashes;
        private int _SelectedLine = -1;

        public override void Init()
        {
            base.Init();
            _SelectableHashes = new int[_Lines];
            var texts = new List<string> { _TextTitleLeft, _TextTitleCenter, _TextTitleRight };

            _TextFields = new string[_Lines * 2];
            for (int i = 0; i < _Lines; i++)
            {
                _TextFields[i] = "TextLineTitle" + (i + 1);
                texts.Add(_TextFields[i]);
            }
            for (int i = 5; i < _Lines * 2; i++)
            {
                _TextFields[i] = "TextLineStart" + (i - _Lines + 1);
                texts.Add(_TextFields[i]);
            }
            _ThemeTexts = texts.ToArray();
            _NeedUpdate = true;
        }

        public override void LoadTheme(string xmlPath)
        {
            _Themex = xmlPath;
            base.LoadTheme(xmlPath);
        }

        public override void ReloadTheme(string xmlPath)
        {
            base.ReloadTheme(xmlPath);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.Key != Keys.None)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Up:
                        scrollList(-1);
                        return true;
                    case Keys.Down:
                        scrollList(1);
                        return true;
                    case Keys.Left:
                        return base.HandleInput(keyEvent);
                    case Keys.Right:
                        return base.HandleInput(keyEvent);
                    case Keys.Enter:
                        if (_SelectedLine > -1)
                        {
                            _ShowDetails(_SelectedLine);
                        }
                        break;
                    case Keys.Back:
                    case Keys.Escape:
                        _EndParty();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            //base.HandleMouse(mouseEvent);
            float z = CBase.Settings.GetZFar();
            int element = -1;
            for (int i = 0; i < _Elements.Count; i++)
            {
                if (!_Elements[i].Type.ToString().Equals("Static"))
                    continue;
                if (!_IsElementVisible(i))
                    continue;
                if (!_IsMouseOverElementOnScreen(mouseEvent.X, mouseEvent.Y, _Elements[i]))
                    continue;
                if (_GetZValueOnScreen(i) > z)
                    continue;
                element = i;
            }
            if (element > -1)
            {
                int theline = _GetSelectableLine(element);
                if (theline > -1)
                {
                    _SetSelectedLine(theline);
                }
            }
            else { _SetSelectedLine(-1); }

            if (mouseEvent.LB && _SelectedLine > -1)
            {
                _ShowDetails(_SelectedLine);
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            //hiding everything
            for (int i = 0; i < _Lines; i++)
            {
                _SelectableHashes[i] = _Statics["StaticLine" + (i + 1)].GetHashCode();
                _Texts[_TextFields[i]].Visible = false;
                _Texts[_TextFields[i + _Lines]].Visible = false;
                _Statics["StaticLine" + (i + 1)].Visible = false;
                _Statics["StaticLine" + (i + 1)].Highlighted = true;
                _Statics["StaticIconPlayable" + (i + 1)].Visible = false;
                _Statics["StaticIconClosed" + (i + 1)].Visible = false;
                _Statics["StaticLineDisabled" + (i + 1)].Visible = false;
                _Statics["StaticLineSelected" + (i + 1)].Visible = false;
            }

            _Texts[_TextTitleCenter].Text = CCommunity.getName();

            _initGame();
        }

        public override bool UpdateGame()
        {
            if (_NeedUpdate == true)
            {
                RenderContestList();
            }
            _NeedUpdate = false;
            return true;
        }

        public void RenderContestList()
        {
            for (int i = 0; i < _Lines; i++)
            {
                if (_ScrollPos + i < _ContestList.Count)
                {
                    _Statics["StaticLine" + (i + 1)].Visible = true;
                    //_Statics["StaticLineSelected" + (i + 1)].Visible = false;

                    _Texts[_TextFields[i]].Text = _ContestList[i + _ScrollPos].name;
                    _Texts[_TextFields[i]].Visible = true;
                    string desc = "Start: " + _ContestList[i + _ScrollPos].startDate.ToString();
                    desc += " | GameMode: Valami | Difficulty: Hard";
                    _Texts[_TextFields[i + _Lines]].Text = desc;
                    _Texts[_TextFields[i + _Lines]].Visible = true;

                    if (_ContestList[i + _ScrollPos].accessible > 0)
                    {
                        _Statics["StaticIconPlayable" + (i + 1)].Visible = true;
                        _Statics["StaticIconClosed" + (i + 1)].Visible = false;
                        _Statics["StaticLineDisabled" + (i + 1)].Visible = false;
                    }
                    else
                    {
                        _Statics["StaticIconPlayable" + (i + 1)].Visible = false;
                        _Statics["StaticIconClosed" + (i + 1)].Visible = true;
                        _Statics["StaticLineDisabled" + (i + 1)].Visible = true;
                    }
                }
                else
                {
                    _Texts[_TextFields[i]].Visible = false;
                    _Texts[_TextFields[i + _Lines]].Visible = false;
                    _Statics["StaticLine" + (i + 1)].Visible = false;
                    _Statics["StaticIconPlayable" + (i + 1)].Visible = false;
                    _Statics["StaticIconClosed" + (i + 1)].Visible = false;
                    _Statics["StaticLineDisabled" + (i + 1)].Visible = false;
                    _Statics["StaticLineSelected" + (i + 1)].Visible = false;
                }
            }

            if (_SelectedLine < 0)
            {
                _Texts[_TextTitleRight].Text = _Translate("TR_SCREENMAIN_EVENTS", new KeyValuePair<string, string>[2] { 
                    new KeyValuePair<string,string> ("v", "1"), 
                    new KeyValuePair<string,string> ("c", _ContestList.Count.ToString()) 
                 });
            }
            else
            {
                _Texts[_TextTitleRight].Text = _Translate("TR_SCREENMAIN_EVENTS", new KeyValuePair<string, string>[2] { 
                     new KeyValuePair<string,string> ("v", (_SelectedLine + 1 + _ScrollPos).ToString()), 
                     new KeyValuePair<string,string> ("c", _ContestList.Count.ToString()) 
                 });
            }
        }

        private void scrollList(int num)
        {
            if (_SelectedLine == -1)
            {
                _SetSelectedLine(0);
                return;
            }
            if (_SelectedLine == 0 && num < 0)
            {
                _ScrollPos += num;
                _ScrollPos = _ScrollPos.Clamp(0, _ContestList.Count - _Lines, true);
                _NeedUpdate = true;
            }
            else if (_SelectedLine == _Lines - 1 && num > 0)
            {
                _ScrollPos += num;
                _ScrollPos = _ScrollPos.Clamp(0, _ContestList.Count - _Lines, true);
                _NeedUpdate = true;
            }

            int newselected = _SelectedLine + num;
            _SetSelectedLine(newselected.Clamp(0, _Lines - 1, true));
            _Texts[_TextTitleRight].Text = _Translate("TR_SCREENMAIN_EVENTS", new KeyValuePair<string, string>[2] { 
                new KeyValuePair<string,string> ("v", (_SelectedLine + 1 + _ScrollPos).ToString()), 
                new KeyValuePair<string,string> ("c", _ContestList.Count.ToString()) 
             });
        }

        private void _ShowDetails(int sel)
        {

            int index = _ScrollPos + sel;
            if (_ContestList.Count > index && index > -1)
            {
                SComResultContestItem item = _ContestList[index];
                string title = item.name;
                string message = "Start: " + item.startDate.ToString() + "\n";
                message += "GameMode: Normal, Difficulty: Hard \n";
                message += " \nEvent description: " + item.description;

                if (item.accessible > 0)
                {
                    showConfirmPopup(item.name, message, delegate(SPopupGeneralEvent eventData)
                    {
                        if (eventData.name.IndexOf("onKey") > -1)
                        {
                            if (eventData.name.Equals("onKeyReturn"))
                            {
                                if (eventData.target != null)
                                {
                                    if (eventData.target.Equals("ButtonYes"))
                                    {
                                        hidePopup();
                                        _CheckPlaylist(item);
                                    }
                                    else
                                    {
                                        hidePopup();
                                    }
                                }
                            }
                            else { hidePopup(); }
                        }
                        else if (eventData.target != null && eventData.target.IndexOf("ButtonYes") > -1)
                        {
                            hidePopup();
                            _CheckPlaylist(item);
                        }
                    });
                }
                else
                {
                    showAlertPopup(item.name, message, delegate(SPopupGeneralEvent eventData)
                    {
                        hidePopup();
                    });
                }
            }
        }

        private void _initGame()
        {
            if (CCommunity.isEnabled())
            {
                //auth profile
                if (!CCommunity.hasAuthProfile())
                {
                    showAlertPopup("TR_ERROR", _Translate("TR_ERROR_NONETSTATPROFILE"), delegate(SPopupGeneralEvent eventData)
                    {
                        if (eventData.name.IndexOf("onKey") > -1)
                        {
                            hidePopup();
                            CBase.Graphics.FadeTo(EScreen.Party);
                        }
                    });
                }
                else
                {
                    showLoadingPopup("TR_SCREENMAIN_LOADING", delegate(SPopupGeneralEvent eventData)
                    {
                        hidePopup();
                        CBase.Graphics.FadeTo(EScreen.Party);
                    });
                    CCommunity.getContestsAsync(_GetContestsResult);
                }
            }
            else
            {
                showAlertPopup("TR_ERROR", _Translate("TR_ERROR_NETSTATDISABLED"), delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.name.IndexOf("onKey") > -1)
                    {
                        hidePopup();
                        CBase.Graphics.FadeTo(EScreen.Party);
                    }
                });
            }
        }

        private void _GetContestsResult(SComResultContestList r)
        {
            hidePopup();
            if (r.status == 1)
            {
                if (r.result != null && r.result.Count > 0)
                {
                    _ContestList = r.result;
                    _NeedUpdate = true;
                }
                else
                {
                    showAlertPopup("TR_SCREENMAIN_LOADING", String.IsNullOrEmpty(r.message) ? _Translate("TR_ERROR_NOEVENT") : r.message, delegate(SPopupGeneralEvent eventData)
                    {
                        if (eventData.name.IndexOf("onKey") > -1)
                        {
                            hidePopup();
                            CBase.Graphics.FadeTo(EScreen.Party);
                        }
                        else if (eventData.target != null && eventData.target.IndexOf("ButtonOk") > -1)
                        {
                            hidePopup();
                            CBase.Graphics.FadeTo(EScreen.Party);
                        }
                    });
                }
            }
            else
            {
                showAlertPopup("TR_ERROR", r.message, delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.name.IndexOf("onKey") > -1)
                    {
                        hidePopup();
                        CBase.Graphics.FadeTo(EScreen.Party);
                    }
                });
            }
        }

        private void _CheckPlaylist(SComResultContestItem item)
        {
            hidePopup();
            showLoadingPopup("TR_SCREENMAIN_LOADINGPLAYLIST", delegate(SPopupGeneralEvent eventData) { });
            CCommunity.getCPlaylistAsync(item.id, _PlaylistLoaded);

            _PartyMode.GameData.SelectedContest = item;
            _PartyMode.GameData.SelectedContestId = item.id;
        }

        private List<SComResultCPlaylistItem> downloadables;
        private void _PlaylistLoaded(SComResultCPlaylist r)
        {
            downloadables = new List<SComResultCPlaylistItem>();
            if (r.status == 1)
            {
                if (r.result != null && r.result.Count > 0)
                {
                    CComCLib.addRemotePlaylist(r.result, r.playlistId);

                    showLoadingPopup(_Translate("TR_SCREENMAIN_CHECKINGLAYLIST").Replace("%c", r.result.Count.ToString()), delegate(SPopupGeneralEvent eventData) { });

                    if (CComCLib.ComparePlaylist(r.playlistId) == true)
                    {
                        hidePopup();
                        //Everythings ok, go to player selection screen
                    }
                    else
                    {
                        string message = "";
                        message += _Translate("TR_STATUS_SONGSCOUNT").Replace("%c", CComCLib.CompareInfo.count.ToString()) + "\n";
                        int correctable = 0;

                        if (CComCLib.CompareInfo.missing != null && CComCLib.CompareInfo.missing.Count > 0)
                        {
                            message += " \n" + _setTextLine(_Translate("TR_STATUS_MISSING"), 85) + "\n";
                            for (int i = 0; i < CComCLib.CompareInfo.missing.Count; i++)
                            {
                                if (CComCLib.CompareInfo.missing[i].files.Length > 0 && !String.IsNullOrEmpty(CComCLib.CompareInfo.missing[i].txtUrl))
                                {
                                    correctable++;
                                    downloadables.Add(CComCLib.CompareInfo.missing[i]);
                                    message += CComCLib.CompareInfo.missing[i].artist + " - " + CComCLib.CompareInfo.missing[i].title + "\n";
                                }
                                else
                                {
                                    message += CComCLib.CompareInfo.missing[i].artist + " - " + CComCLib.CompareInfo.missing[i].title + " (!)\n";
                                }
                               
                            }
                        }
                        if (CComCLib.CompareInfo.wrongHashes != null && CComCLib.CompareInfo.wrongHashes.Count > 0)
                        {
                            message += " \n" + _setTextLine(_Translate("TR_STATUS_WRONG"), 85) + "\n";
                            for (int i = 0; i < CComCLib.CompareInfo.wrongHashes.Count; i++)
                            {
                                if (!String.IsNullOrEmpty(CComCLib.CompareInfo.wrongHashes[i].txtUrl) && CComCLib.CompareInfo.wrongHashes[i].localId > 0)
                                {
                                    correctable++;
                                    downloadables.Add(CComCLib.CompareInfo.wrongHashes[i]);
                                    message += CComCLib.CompareInfo.wrongHashes[i].artist + " - " + CComCLib.CompareInfo.wrongHashes[i].title + "\n";
                                }
                                else
                                {
                                    message += CComCLib.CompareInfo.wrongHashes[i].artist + " - " + CComCLib.CompareInfo.wrongHashes[i].title + " (!)\n";
                                }
                               
                            }
                        }

                        //download new song versions (txt files or files)
                        if (correctable > 0)
                        {
                            //can continue
                            showConfirmPopup("TR_STATUS_DL", message, delegate(SPopupGeneralEvent eventData)
                            {
                                if (eventData.name.IndexOf("onKey") > -1)
                                {
                                    hidePopup();
                                    if (eventData.target != null && eventData.target.Equals("ButtonYes"))
                                    {

                                        //_PartyMode.Next();
                                        _CheckDownloadables();
                                    }
                                    else if (eventData.target != null && eventData.target.Equals("ButtonNo"))
                                    {
                                        hidePopup();
                                    }
                                    else if (eventData.name.Equals("onKeyEscape") || eventData.name.Equals("onKeyBack"))
                                    {
                                        hidePopup();
                                    }
                                }
                                //mouse
                                else
                                {
                                    hidePopup();
                                    if (eventData.target != null && eventData.target.Equals("ButtonYes"))
                                    {
                                        //_PartyMode.Next();
                                        _CheckDownloadables();
                                    }
                                    else
                                    {
                                        hidePopup();
                                    }
                                }

                            }, CBase.Language.Translate("TR_BUTTON_DL"));
                        }
                        else
                        {
                            //cant continue
                            showAlertPopup("TR_ERROR_MISSINGSONGS", message, delegate(SPopupGeneralEvent eventData)
                            {
                                hidePopup();
                            });
                        }
                    }

                    return;
                }
            }
            showAlertPopup("TR_SCREENMAIN_LOADINGPLAYLIST", String.IsNullOrEmpty(r.message) ? _Translate("TR_ERROR_PLAYLIST") : r.message, delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.name.IndexOf("onKey") > -1)
                {
                    hidePopup();
                }
                else if (eventData.target != null && eventData.target.IndexOf("ButtonOk") > -1)
                {
                    hidePopup();
                }
            });
        }

        private string _setTextLine(string text, int lenght)
        {
            int l = text.Length;
            for (int i = l; i < lenght; i++)
            {
                text += "_";
            }

            return text;
        }

        /*
         * TODO: Download missing / wrong files automatically
         */

        private int  _currentDownloadIndex = -1;
        private bool _CheckDownloadables()
        {
            if (downloadables.Count > 0 && _currentDownloadIndex+1 < downloadables.Count)
            {
                _downloadSongs();
                return true;
            }
            else
            {
                if (_currentDownloadIndex > -1)
                {
                    CBase.Graphics.FadeTo(EScreen.Load);
                }
            }
            return false;
        }

        private void _DownloadFilesCallback (CFileDlEvent eventData){
            SComResultCPlaylistItem item = downloadables[_currentDownloadIndex];
          
            if (eventData.status == CFileDlEventStatus.Progress)
            {
               // Console.WriteLine(eventData.target.fileType + "=> " + eventData.BytesPercent + "%");
            }
            else if (eventData.status == CFileDlEventStatus.Success)
            {
                //on success
                if (item.localId > 0)
                {
                    var song = CBase.Songs.GetSongByID(item.localId);
                    File.Move(Path.Combine(song.Folder, song.FileName), Path.Combine(song.Folder, song.FileName.Replace(".txt", ".txt.old")));

                    //File.Move(Path.Combine(song.Folder, eventData.target.fileName), Path.Combine(song.Folder, song.FileName));
                }
                else { 
                    Console.WriteLine("Downloaded "+eventData.target.fileName);
                }

                if (eventData.FilesCount == eventData.CurrentFile+1)
                {
                     _CheckDownloadables();
                }
            }
            else if (eventData.status == CFileDlEventStatus.Error)
            {
                Console.WriteLine("Error: " + eventData.message);
                //_CheckDownloadables();
                //error
            }
            else
            {
                Console.WriteLine("Status: " + eventData.message);
            }
        }

        private CFileDownloader fd;
        private bool _downloadSongs()
        {
            fd = new CFileDownloader();
            fd.init();
            if (_currentDownloadIndex < downloadables.Count)
            {
                _currentDownloadIndex++;
                SComResultCPlaylistItem item = downloadables[_currentDownloadIndex];


                Console.WriteLine("Artis: " + item.artist+ " / "+item.title+ " at "+item.txtUrl);

                //txt update
                if (item.localId > 0)
                {
                    var song = CBase.Songs.GetSongByID(item.localId);
                    if (song != null)
                    {
                        //Console.WriteLine("Download from: " + item.txtUrl + " to " + path);

                        if (fd.parseUrl(item.txtUrl))
                        {
                            fd.prepareDownload(song.Folder, song.FileName.Replace(".txt", "_c"), delegate(Boolean status)
                            {
                                if (status)
                                {
                                    fd.DownloadFiles(_DownloadFilesCallback);
                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine("Bad url ... " + item.txtUrl);
                        }
                    }
                }
                //new song
                else
                {
                    //create folder
                    string firstSongFolder = null;
                    string newSongFolder = null;
                    foreach (string folder in CBase.Config.GetSongFolders()) {
                        if (firstSongFolder == null) 
                            firstSongFolder = folder;
                    }

                    if (firstSongFolder != null) {
                        if (!Directory.Exists(Path.Combine(firstSongFolder, "Community"))) {
                            Directory.CreateDirectory(Path.Combine(firstSongFolder, "Community"));
                        }
                        newSongFolder = Path.Combine(Path.Combine(firstSongFolder,"Community"), item.artist+" - "+item.title);
                        if (!Directory.Exists(newSongFolder)) {
                             Directory.CreateDirectory(newSongFolder);
                        }

                        for (int i = 0; i < item.files.Length; i++) {
                             if (fd.parseUrl(item.files[i])) {
                                 Console.WriteLine("preparing: "+item.files[i]);
                                 fd.prepareDownload(newSongFolder, item.title, delegate(Boolean status) {
                                     Console.WriteLine("Prepared ... "+status);
                                 });
                             }
                        }
                        //add txt file
                        if (fd.parseUrl(item.txtUrl)) {
                            fd.prepareDownload(newSongFolder, item.title, delegate(Boolean status) {
                                Console.WriteLine("Prepared txt ... " + status);
                            });
                        }

                        Console.WriteLine("Start download process ... ");

                        //download files...
                        fd.DownloadFiles(_DownloadFilesCallback);
                    }
                }
            }
            return false;
        }

        private void showLoadingPopup(string title, Action<SPopupGeneralEvent> callback)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyEscape", callback);

            data.type = EPopupGeneralType.Loading;
            data.size = EPopupGeneralSize.Small;
            data.TextTitle = _Translate(title);

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private void showAlertPopup(string title, string message, Action<SPopupGeneralEvent> callback)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyBack,onKeyEscape,onMouseLB", callback);

            data.type = EPopupGeneralType.Alert;
            data.size = EPopupGeneralSize.Medium;
            data.TextTitle = _Translate(title);
            data.TextMessage = message;
            data.ButtonOkLabel = _Translate("TR_BUTTON_OK");

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private void showConfirmPopup(string title, string message, Action<SPopupGeneralEvent> callback, string buttonYesLabel = null, string buttonNoLabel = null)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyBack,onKeyEscape,onMouseLB", callback);

            data.type = EPopupGeneralType.Confirm;
            data.size = EPopupGeneralSize.Big;
            data.TextTitle = _Translate(title);
            data.TextMessage = message;
            data.ButtonYesLabel = String.IsNullOrEmpty(buttonYesLabel) ? _Translate("TR_BUTTON_PLAY") : _Translate(buttonYesLabel);
            data.ButtonNoLabel = String.IsNullOrEmpty(buttonNoLabel) ? "TR_BUTTON_BACK" : _Translate(buttonNoLabel);

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private void hidePopup()
        {
            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
        }


        private void _EndParty()
        {
            CBase.Graphics.FadeTo(EScreen.Party);
        }

        private bool _IsMouseOverElementOnScreen(int x, int y, CInteraction interact)
        {
            bool result = CHelper.IsInBounds(_GetElement(interact).Rect, x, y);
            if (result)
                return true;

            return false;
        }

        private bool _IsSelectableOnScreen(int element)
        {
            IMenuElement el = _GetElement(_Elements[element]);
            return el != null && (el.Selectable || CBase.Settings.GetProgramState() == EProgramState.EditTheme);
        }

        private bool _IsElementVisible(int element)
        {
            IMenuElement el = _GetElement(_Elements[element]);
            return el != null && (el.Visible);
        }

        private float _GetZValueOnScreen(int element)
        {
            return _GetElement(_Elements[element]).Rect.Z;
        }

        private int _GetSelectableLine(int element)
        {
            IMenuElement el = _GetElement(_Elements[element]);
            int currHash = el.GetHashCode();
            for (int i = 0; i < _Lines; i++)
            {
                if (_SelectableHashes[i] == currHash)
                {
                    return i;
                }
            }
            return -1;
        }

        private void _SetSelectedLine(int line)
        {
            if (_SelectedLine == line) { return; }
            for (int i = 0; i < _Lines; i++)
            {
                if (line == i)
                {
                    _Statics["StaticLineSelected" + (i + 1)].Visible = true;
                }
                else
                {
                    _Statics["StaticLineSelected" + (i + 1)].Visible = false;
                }
            }
            _SelectedLine = line;
        }

        private string _Translate(string str, KeyValuePair<string, string>[] kvpair = null)
        {
            string translated = CBase.Language.Translate(str, _PartyMode.ID);
            if (kvpair != null)
            {
                if (kvpair.Length > 0)
                {
                    for (int i = 0; i < kvpair.Length; i++)
                    {
                        translated = translated.Replace("%" + kvpair[i].Key, kvpair[i].Value);
                    }
                }
            }
            return translated;
        }
    }
}