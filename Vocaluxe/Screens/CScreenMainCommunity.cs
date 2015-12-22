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
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Community;


namespace Vocaluxe.Screens
{
    public class CScreenMainCommunity : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private class CCTilesHelper
        {
            public int newsId;
            public int status;
            public int songId;
            public float alpha = 1f;
        }

        private const string _ButtonLogin = "ButtonLogin";
        private const string _ButtonCMenu1 = "ButtonCMenu1";
        private const string _ButtonCMenu2 = "ButtonCMenu2";
        private const string _ButtonCMenu3 = "ButtonCMenu3";
        private const string _ButtonCMenu4 = "ButtonCMenu4";

        private const string _TextCSongTitle = "TextCSongTitle";
        private const string _TextCNewSongs = "TextCNewSongs";
        private const string _TextCEvents = "TextCEvents";
        private const string _TextCLoading = "TextCLoading";

        private const string _StaticSongsArea = "CommunityNewSongsArea";
        private const string _StaticComMenu = "StaticComMenu";
        private const string _StaticCSongBg = "CommunityNewSongsTitle";
        private const string _StaticLoading = "CommunityLoading";
        private const string _StaticCDropdownBg = "CommunityMenuBg";
        private const string _LoginAvatar = "StaticLoginAvatar";

        private string[] _DropDownMenuButtons;

        private CAvatar _Avatar;
        private string _Username;
        private Boolean _CComUpdated = false;
        private Boolean _NewsAreaVisible = false;
        private Boolean _NeedUpdate = false;
        private Boolean _LoadingAnimDirection = false;
        private Boolean _ThemeLoaded = false;
        private Boolean _SongsChecked = false;
        private Boolean _DataLoaded = false;
        private int _ActiveTile = -1;
        private SComSongsResult _News;
        private List<CStatic> _Tiles;
        private List<CCTilesHelper> _TilesHelper;
        private CTextureRef _DefaultTileTexture;


        //TODO: put this to theme xml
        private int cols = 5;
        private int rows = 4;
        private int colspacing = 0;
        private int rowspacing = 0;
        private int tileWidth;
        private int tileHeight;

        public string[] MergeThemeElements(string type, string[] initialData)
        {
            int nsc = initialData.Length;
            string[] data = new string[0];
            if (type.Equals("Static")) { nsc += _ThemeStatics.Length; data = _ThemeStatics; }
            else if (type.Equals("Text")) { nsc += _ThemeTexts.Length; data = _ThemeTexts; }
            else if (type.Equals("Button")) { nsc += _ThemeButtons.Length; data = _ThemeButtons; }

            string[] ns = new string[nsc];
            int x = 0;
            for (int i = 0; i < initialData.Length; i++)
            {
                ns[x] = initialData[i];
                x++;
            }
            for (int i = 0; i < data.Length; i++)
            {
                ns[x] = data[i];
                x++;
            }
            return ns;
        }

        public override void Init()
        {
            base.Init();
            _DropDownMenuButtons = new string[] { _ButtonCMenu1, _ButtonCMenu2, _ButtonCMenu3, _ButtonCMenu4 };
            _ThemeButtons = new string[] { _ButtonLogin, _ButtonCMenu1, _ButtonCMenu2, _ButtonCMenu3, _ButtonCMenu4 };
            _ThemeTexts = new string[] { _TextCLoading, _TextCEvents, _TextCNewSongs, _TextCSongTitle };
            _ThemeStatics = new string[] { _StaticCDropdownBg, _LoginAvatar, _StaticComMenu, _StaticSongsArea, _StaticCSongBg, _StaticLoading };
            CommunityInit();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _ThemeLoaded = true;
            _Statics[_StaticSongsArea].Visible = false;

            _InitTiles();
            _RenderDropDownMenu(false);
            _HideNews();
            if (!CCommunity.isEnabled())
            {
                _Statics[_StaticLoading].Visible = false;
                _Texts[_TextCLoading].Visible = false;
                _Buttons[_ButtonLogin].Visible = false;
            }
        }


        public override void Draw()
        {
            base.Draw();
            if (_NewsAreaVisible)
            {
                int x = 0;
                foreach (CStatic tile in _Tiles)
                {
                    if (tile.Visible)
                    {
                        if (tile.Texture != _DefaultTileTexture && tile.Alpha != _TilesHelper[x].alpha)
                        {
                            var alpha = tile.Alpha;
                            alpha += (_TilesHelper[x].alpha - alpha) * 0.1f;
                            tile.Alpha = alpha.Clamp(0f, _TilesHelper[x].alpha);
                        }
                       
                        if (tile.Selected)
                        {
                            tile.Alpha = 1;
                            tile.Draw(EAspect.Crop, 1.2f, -0.1f);
                        }
                        else
                        {
                            EAspect aspect = EAspect.Stretch;
                            tile.Draw(aspect);
                        }
                    }
                    x++;
                }
            }
        }

        public override void OnShow()
        {
            base.OnShow();
            _CheckSongStatus();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Enter:
                        if (_Buttons[_ButtonLogin].Selected)
                            CommunityLogin();

                        if (_Buttons[_ButtonCMenu1].Selected)
                            CommunityAction(1);

                        if (_Buttons[_ButtonCMenu2].Selected)
                            CommunityAction(2);

                        if (_Buttons[_ButtonCMenu3].Selected)
                            CommunityAction(3);

                        if (_Buttons[_ButtonCMenu4].Selected)
                            CommunityAction(4);
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (CCommunity.isEnabled())
            {
                if (CHelper.IsInBounds(_Statics[_StaticSongsArea].Rect, mouseEvent))
                {
                    int foundTile = -1;
                    for (int i = 0; i < _Tiles.Count; i++)
                    {
                        if (foundTile == -1)
                        {
                            if (CHelper.IsInBounds(_Tiles[i].Rect, mouseEvent))
                            {
                                foundTile = i;
                                _SetActiveTile(i);
                            }
                            else
                            {
                                _Tiles[i].Selected = false;
                            }
                        }
                        else
                        {
                            _Tiles[i].Selected = false;
                        }
                    }

                    if (foundTile == -1)
                        _SetActiveTile(-1);

                    //click
                    if (mouseEvent.LB && foundTile > -1)
                    {
                        _OpenTileInfo(foundTile);
                        return true;
                    }
                }
                else
                {
                    for (int i = 0; i < _Tiles.Count; i++)
                    {
                        _Tiles[i].Selected = false;
                    }
                }
            }


            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonLogin].Selected)
                    CommunityLogin();

                if (_Buttons[_ButtonCMenu1].Selected && CCommunity.isLoggedIn())
                    CommunityAction(1);

                if (_Buttons[_ButtonCMenu2].Selected && CCommunity.isLoggedIn())
                    CommunityAction(2);

                if (_Buttons[_ButtonCMenu3].Selected && CCommunity.isLoggedIn())
                    CommunityAction(3);

                if (_Buttons[_ButtonCMenu4].Selected && CCommunity.isLoggedIn())
                    CommunityAction(4);
            }
            else if (_IsMouseOverCurSelection(mouseEvent))
            {
                if (CCommunity.isLoggedIn() && _Buttons[_ButtonLogin].Selected)
                {
                    _RenderDropDownMenu(true);
                }
            }
            else
            {
                _RenderDropDownMenu(false);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (_NeedUpdate)
            {
                for (int i = 0; i < _Tiles.Count; i++)
                {
                    if (_Tiles[i].Texture == _DefaultTileTexture)
                    {
                        if (_News.items != null)
                        {
                            _Tiles[i].Visible = true;
                            if (!String.IsNullOrWhiteSpace(_News.items[i].coverUrl)) {
                                CTextureLoader.LoadTo(_News.items[i].coverUrl, _Tiles[i], delegate(bool status, bool fromcache, CStatic element)
                                {
                                    if (!status)
                                    {
                                        element.Texture = CBase.Cover.GenerateCover(_News.items[i].artist, ECoverGeneratorType.Artist, null);
                                    }
                                });
                            }
                            else
                            {
                                _Tiles[i].Texture = CBase.Cover.GenerateCover(_News.items[i].artist, ECoverGeneratorType.Artist, null);
                            }
                        }
                    }
                }
                _NeedUpdate = false;
            }

            if (_CComUpdated)
            {
                CommunityDisplay();
                _CComUpdated = false;
            }

            //loading indicator
            if (_Statics[_StaticLoading].Visible == true)
            {
                var alpha = _Statics[_StaticLoading].Alpha;
                if (!_LoadingAnimDirection)
                {
                    if (_Statics[_StaticLoading].Alpha == 1) { _LoadingAnimDirection = true; }
                    else
                    {
                        alpha += 0.05f;
                    }
                }
                else
                {
                    if (_Statics[_StaticLoading].Alpha == 0) { _LoadingAnimDirection = false; }
                    else
                    {
                        alpha -= 0.05f;
                    }
                }
                _Statics[_StaticLoading].Alpha = alpha.Clamp(0f, 1f);
            }


            return true;
        }

        #region NewsArea

        private void _ShowNews()
        {
            _NewsAreaVisible = true;
            _Texts[_TextCSongTitle].Visible = true;
            _Texts[_TextCEvents].Visible = true;
            _Texts[_TextCNewSongs].Visible = true;
            _Statics[_StaticCSongBg].Visible = true;
        }

        private void _HideNews()
        {
            _NewsAreaVisible = false;
            _Texts[_TextCSongTitle].Visible = false;
            _Texts[_TextCEvents].Visible = false;
            _Texts[_TextCNewSongs].Visible = false;
            _Statics[_StaticCSongBg].Visible = false;
        }

        public void _SetActiveTile(int id)
        {
            if (id > -1 && id < _Tiles.Count && _DataLoaded)
            {
                _ActiveTile = id;
                _Tiles[id].Selected = true;

                if (id > -1 && id < _News.items.Length)
                {
                    _Texts[_TextCSongTitle].Text = _News.items[id].artist + " - " + _News.items[id].title;
                }
            }
            else
            {
                _ActiveTile = -1;
                _Texts[_TextCSongTitle].Text = "";
            }
        }

        private void _LoadNews()
        {
            if (_News.items == null || _News.items.Length == 0)
            {
                CCommunity.getNewsAsync(delegate(SComSongsResult data)
                {
                    if (data.items != null && data.items.Length > 0)
                    {
                        _News = data;
                        _NeedUpdate = true;
                        _DataLoaded = true;

                        if (!_SongsChecked)
                            _CheckSongStatus();
                    }
                });
            }
        }

        public void _OpenTileInfo(int tileId)
        {
            if (tileId > -1 && _SongsChecked)
            {
                var d = _TilesHelper.Find(x => x.newsId == tileId);
                if (d != null)
                {
                    if (d.status == 0)
                    {
                        _SongIsUptodate(d.newsId, d.songId);
                    }
                    else if (d.status == 1)
                    {
                        _NewSong(d.newsId);
                    }
                    else if (d.status == 2)
                    {
                        _UpdateSong(d.newsId, d.songId);
                    }
                }
            }
        }

        private void _InitTiles()
        {
            _TilesHelper = new List<CCTilesHelper>();

            tileWidth = (int)((_Statics[_StaticSongsArea].W - colspacing * (cols - 1)) / cols);
            tileHeight = (int)((_Statics[_StaticSongsArea].H - rowspacing * (rows - 1)) / rows);
            _DefaultTileTexture = _Statics[_StaticSongsArea].Texture;

            SColorF _Color = new SColorF(1f, 1f, 1f, 1f);
            _Tiles = new List<CStatic>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var rect = new SRectF(_Statics[_StaticSongsArea].X + j * (tileWidth + colspacing), _Statics[_StaticSongsArea].Y + i * (tileHeight + rowspacing), tileWidth, tileHeight, _Statics[_StaticSongsArea].Z);
                    var tile = new CStatic(0, _DefaultTileTexture, _Color, rect);
                    tile.Alpha = 0f;
                    tile.Visible = false;
                    _Tiles.Add(tile);
                }
            }
           
        }

        private void _CheckSongStatus()
        {
            if (_News.items != null && _News.items.Length > 0)
            {
                for (var x = 0; x < _News.items.Length; x++)
                {
                    if (_TilesHelper.Count < _News.items.Length)
                    {
                        var helper = new CCTilesHelper();
                        helper.newsId = x;
                        _TilesHelper.Add(helper);
                    }

                    int songId = CCommunitySong.FindSongByHash(_News.items[x].txtHash);
                    if (songId == -1)
                    {
                        if (_News.items[x].knownHashes != null)
                        {
                            songId = CCommunitySong.FindSongByHash(_News.items[x].knownHashes);
                            if (songId > -1)
                            {
                                _TilesHelper[x].songId = songId;
                                _TilesHelper[x].status = 2;
                            }
                            else { _TilesHelper[x].status = 1; }
                        }
                    }
                    else if (songId > -1)
                    {
                        _TilesHelper[x].songId = songId;
                        _TilesHelper[x].status = 0;
                        _TilesHelper[x].alpha = 0.5f;
                    }
                   
                }
                _SongsChecked = true;
            }
        }

        private void _UpdateSongStatus(int id)
        {
            var d = _TilesHelper.Find(x => x.newsId == id);
            if (d != null)
            {
                int songId = CCommunitySong.FindSongByHash(_News.items[id].txtHash);
                if (songId > 0)
                {
                    d.status = 0;
                    d.songId = songId;
                    d.alpha = 0.5f;
                }
            }
        }
        #endregion

        #region popups
        private void _UpdateSong(int id, int songId)
        {
            CPopupHelper.Confirm("TR_COMMUNITY_SONGUPDATEFOUND", "TR_COMMUNITY_SONGUPDATEFOUND_TEXT", delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.Name.Equals("onKeyEscape")
                    || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                    || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                )
                {
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                }

                if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                  || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                )
                {
                    var txtFile = _News.items[id].fileList.FirstOrDefault(x => x.Key == "txt").Value;
                    if (txtFile != null)
                    {
                        var txtUrl = txtFile.FirstOrDefault(x => x.Key == "url").Value;
                        if (txtUrl != null)
                        {
                            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                            CCommunitySong.UpdateSongTextFile(txtUrl, songId, delegate(bool status)
                            {
                                if (status)
                                {
                                    _UpdateSongStatus(id);
                                }
                            });
                            return;
                        }
                    }

                    CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_ERROR_UPDATETXT0", null, new SPopupGeneral { Size = EPopupGeneralSize.Medium });
                }
            });
        }

        private void _SongIsUptodate(int id, int songId)
        {
            CPopupHelper.Confirm("TR_COMMUNITY_SONGISUPTODATE", "TR_COMMUNITY_SONGISUPTODATE_TEXT", delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.Name.Equals("onKeyEscape")
                    || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                    || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                )
                {
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                   || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                )
                {
                    CBase.Game.Reset();
                    CBase.Game.ClearSongs();
                    var song = CBase.Songs.GetSongByID(songId);
                    EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                    if (song.IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;

                    CBase.Game.AddSong(songId, gm);
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    CBase.Graphics.FadeTo(EScreen.Names);
                }
            });

            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            var data = popup.GetDisplayData();
            data.ButtonYesLabel = CBase.Language.Translate("TR_COMMUNITY_SINGIT");
            popup.SetDisplayData(data);
        }

        private void _NewSong(int id)
        {
            if (_News.items[id].hasDownloadSupport)
            {
                CPopupHelper.Confirm("TR_COMMUNITY_DLSONG", "TR_COMMUNITY_DLSONGTEXT", delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.Name.Equals("onKeyEscape")
                        || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                    )
                    {
                        CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                    if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                    )
                    {
                        _DownloadSongHelper(id);
                    }
                });
            }
            else
            {
                CPopupHelper.Alert("TR_COMMUNITY_DLSONG", "TR_COMMUNITY_ERROR_NODLSUPPORT", null, new SPopupGeneral { Size = EPopupGeneralSize.Medium });
            }
        }
        

        private void _DownloadSongHelper(int id)
        {
            if (!String.IsNullOrWhiteSpace(_News.items[id].licenceUrl))
            {
                CPopupHelper.Loading("TR_COMMUNITY_LOADING", "");
                CCommunity.getTextAsync(_News.items[id].licenceUrl, delegate(string response)
                {
                    if (!String.IsNullOrWhiteSpace(response))
                    {
                        SPopupGeneral data = new SPopupGeneral();
                        data.ButtonYesLabel = CBase.Language.Translate("TR_COMMUNITY_ACCEPT");
                        data.ButtonNoLabel = CBase.Language.Translate("TR_COMMUNITY_REJECT");
                        data.Size = EPopupGeneralSize.Big;

                        CPopupHelper.Confirm("TR_COMMUNITY_LICENCE", response, delegate(SPopupGeneralEvent eventData)
                        {
                            if (eventData.Name.Equals("onKeyEscape")
                                || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                            )
                            {
                                CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                            }
                            if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                            )
                            {
                                _DownloadSelectedSong(id);
                            }
                        }, data);

                    }
                    else
                    {
                        CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_UNKERR", null, new SPopupGeneral { Size = EPopupGeneralSize.Medium });
                    }
                });
            }
            //download
            else
            {
                _DownloadSelectedSong(id);
            }
        }

        private void _DownloadSelectedSong(int id)
        {
            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            CCommunitySong.DownloadSong(_News.items[id], delegate(bool status)
            {
                if (status == true)
                    _CheckSongStatus();
            });
        }

        #endregion

        #region Community
        private void CommunityInit()
        {
            CCommunity.AddEventHandler((Action<SComEvent>)_OnCommunityEvent);
            CCommunity.Init();
            CCommunity.checkConnection();
            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
        }

        private void _RenderDropDownMenu(bool status)
        {
            if (status)
            {
                if (!_Statics[_StaticCDropdownBg].Visible)
                {
                    _Statics[_StaticCDropdownBg].Visible = true;
                    _Buttons[_ButtonLogin].H = _Statics[_StaticCDropdownBg].H;
                    for (int i = 0; i < _DropDownMenuButtons.Length; i++)
                    {
                        _Buttons[_DropDownMenuButtons[i]].Visible = true;
                    }
                }
            }
            else
            {
                if (_Statics[_StaticCDropdownBg].Visible)
                {
                    _Statics[_StaticCDropdownBg].Visible = false;
                    _Buttons[_ButtonLogin].H = 41;
                    for (int i = 0; i < _DropDownMenuButtons.Length; i++)
                    {
                        _Buttons[_DropDownMenuButtons[i]].Visible = false;
                    }
                }
            }
        }

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
            {
                if (CCommunity.isEnabled() && CCommunity.isReadyForAuth())
                {
                    SComAuthUser loggedUser = CCommunity.getCurrentUser();
                    if (loggedUser.profileCreated == 0)
                    {
                        var pid = CCommunity.findMainProfile();
                        if (pid == -1)
                        {
                            //create profile
                        }
                        else
                        {
                            var profile = CProfiles.GetProfile(pid);
                            if (profile.CommunityUUID != CCommunity.config.AuthUUID)
                            {
                                CProfiles.SetCommunityProfile(pid, profile.CommunityUsername, profile.CommunityUUID);
                                CProfiles.SaveProfiles();
                            }
                        }
                    }
                }
            }

        }

        private void _OnCommunityEvent(SComEvent eventData)
        {
            //Community status changed (login, connection, settings, loading)
            _CComUpdated = true;
            if (eventData.eventType == EComEventType.loading) {
                Console.WriteLine(eventData.eventType.ToString() + " : " + eventData.status);
                if (eventData.status == 1)
                {
                    if (_ThemeLoaded)
                    {
                        _Statics[_StaticLoading].Visible = true;
                        _Texts[_TextCLoading].Visible = true;
                        _Statics[_StaticLoading].Alpha = 1;
                    }
                }
                else
                {
                    if (_ThemeLoaded)
                    {
                        _Statics[_StaticLoading].Visible = false;
                        _Texts[_TextCLoading].Visible = false;
                    }
                }
            }

            if (eventData.eventType == EComEventType.loginStatus && eventData.status == 1)
            {
                _LoadNews();
                _ShowNews();
            }

            if (eventData.eventType == EComEventType.loginStatus && eventData.status == 0)
            {
                _HideNews();
            }
        }

        public void CommunityDisplay()
        {
            _Statics[_LoginAvatar].Alpha = 1;
            _Buttons[_ButtonLogin].Text.Alpha = 1;
            //load default avatar
            if (CCommunity.isEnabled() && _Avatar == null)
            {
                foreach (string path in CConfig.ProfileFolders)
                {
                    _Avatar = CAvatar.GetAvatar(System.IO.Path.Combine(path, "Avatar_m.png"));
                    if (_Avatar != null)
                    {
                        _Statics[_LoginAvatar].Texture = _Avatar.Texture;
                        _Statics[_LoginAvatar].Aspect = EAspect.Stretch;
                        break;
                    }
                }
            }

            if (CCommunity.isEnabled())
            {
                _Buttons[_ButtonLogin].Visible = true;
                _Statics[_LoginAvatar].Visible = true;
                _Statics[_StaticComMenu].Visible = true;

                if (CCommunity.isReadyForAuth())
                {
                    SComAuthUser loggedUser = CCommunity.getCurrentUser();
                    if (loggedUser.authenticated)
                    {
                        _ShowNews();
                    }

                    if (loggedUser.profileCreated == -1)
                    {
                        CProfile profile = new CProfile();
                        profile.CommunityUUID = loggedUser.uuid;
                        profile.CommunityUsername = loggedUser.username;
                        profile.PlayerName = loggedUser.displayName;
                        CProfiles.LoadAvatars();

                        if (loggedUser.avatarFile != null)
                        {
                            profile.AvatarFileName = loggedUser.avatarFile;
                        }
                        profile.ID = CProfiles.NewProfile();
                        var cavatar = CProfiles.GetAvatarByFilename(loggedUser.avatarFile);
                        if (cavatar != null)
                            CProfiles.SetAvatar(profile.ID, cavatar.ID);

                        CProfiles.SetPlayerName(profile.ID, profile.PlayerName);
                        CProfiles.SetCommunityProfile(profile.ID, loggedUser.username, loggedUser.uuid);
                        CProfiles.SetActive(profile.ID, EOffOn.TR_CONFIG_ON);
                        CProfiles.SetDifficulty(profile.ID, EGameDifficulty.TR_CONFIG_NORMAL);
                        CProfiles.SetUserRoleProfile(profile.ID, EUserRole.TR_USERROLE_ADMIN);
                        CProfiles.SaveProfiles();
                        loggedUser.profileCreated = profile.ID;
                        CCommunity.setCurrentUser(loggedUser);
                    }
                    else if (loggedUser.profileCreated > 0)
                    {
                        var profile = CProfiles.GetProfile(loggedUser.profileCreated);
                        if (profile.AvatarFileName != null)
                        {
                            _Statics[_LoginAvatar].Texture = profile.Avatar.Texture;
                            _Statics[_LoginAvatar].Aspect = EAspect.Stretch;
                        }

                        if (loggedUser.authenticated)
                        {
                            _Buttons[_ButtonLogin].Text.Text = loggedUser.displayName;
                        }
                        {
                            _Buttons[_ButtonLogin].Text.Text = profile.PlayerName;
                        }
                        if (CCommunity.connectionStatus == false)
                        {
                            _Buttons[_ButtonLogin].Text.Text += " (Offline)";
                            _Buttons[_ButtonLogin].Text.Alpha = 0.6f;
                            _Statics[_LoginAvatar].Alpha = 0.3f;
                        }
                    }
                    else
                    {
                        if (loggedUser.authenticated)
                        {
                            _Buttons[_ButtonLogin].Text.Text = loggedUser.displayName;
                            var pid = CCommunity.findMainProfile();
                        }
                        if (CCommunity.connectionStatus == false)
                        {
                            _Buttons[_ButtonLogin].Text.Text = "Offline mode";
                        }
                    }
                }
                //not ready for auth
                else
                {
                    _Buttons[_ButtonLogin].Text.Text = "TR_SCREENMAIN_TEXT_CLOGIN";
                    _Statics[_LoginAvatar].Visible = true;
                    _Buttons[_ButtonLogin].Visible = true;
                    if (_Avatar != null)
                    {
                        _Statics[_LoginAvatar].Texture = _Avatar.Texture;
                    }
                    _Statics[_StaticLoading].Visible = false;
                    _Texts[_TextCLoading].Visible = false;
                }
            }
            else
            {
                _Statics[_LoginAvatar].Visible = false;
                _Buttons[_ButtonLogin].Visible = false;
                _Statics[_StaticComMenu].Visible = false;
                _HideNews();
            }
        }

        private void CommunityAction(int action)
        {
            switch (action)
            {
                case 1:
                    CGraphics.FadeTo(EScreen.CommunitySongs);
                    break;
                case 2:
                    CGraphics.FadeTo(EScreen.CommunityEvents);
                    break;
                case 3:
                    CGraphics.FadeTo(EScreen.CommunityUpdates);
                    break;
                //logout
                case 4:
                    CPopupHelper.Confirm("TR_COMMUNITY_TXTLOGOUT", CLanguage.Translate("TR_COMMUNITY_TXTLOGOUT_CONFIRM").Replace("%s", CCommunity.getName()), LogoutWindowCallback);
                    break;
            }
        }

        private void CommunityLogin(String emsg = null)
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();

            if (CCommunity.isReadyForAuth())
            {
                if (CCommunity.connectionStatus == true)
                {
                }
                else
                {
                    CPopupHelper.Alert("TR_COMMUNITY_OFFLINE", "TR_COMMUNITY_OFFLINEALERT");
                }
            }
            else
            {
                //create login window
                SPopupGeneral data = new SPopupGeneral();
                data.Username = String.IsNullOrWhiteSpace(_Username) ? _Username : "";
                CPopupHelper.Login(CLanguage.Translate("TR_COMMUNITY_TXTLOGIN").Replace("%s", CCommunity.getName()), emsg, (Action<SPopupGeneralEvent>)LoginWindowCallback, data);
            }
        }

        private void LogoutWindowCallback(SPopupGeneralEvent eventData)
        {
            if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes")) || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes")))
                CCommunity.logout();

            CGraphics.HidePopup(EPopupScreens.PopupGeneral);
        }

        private void LoginWindowCallback(SPopupGeneralEvent eventData)
        {
            if (eventData.Name.Equals("onKeyEscape")
               || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
               || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
            )
            {
                CGraphics.HidePopup(EPopupScreens.PopupGeneral);
            }
            else if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes")))
            {
                var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
                var wndData = popup.GetDisplayData();
                SComQueryAuth auth = new SComQueryAuth();
                if (String.IsNullOrWhiteSpace(wndData.Username) || String.IsNullOrWhiteSpace(wndData.Password))
                {
                    wndData.TextMessage = CLanguage.Translate("TR_COMMUNITY_LOGINREQFIELDS");
                }
                else
                {
                    _Username = auth.username = wndData.Username;
                    auth.password = wndData.Password;
                    CCommunity.authRequestAsync(auth, (Action<SComResponseAuth, SComQueryAuth>)LoginResponse);

                    wndData.TextMessage = CLanguage.Translate("TR_COMMUNITY_LOADING");
                    wndData.Type = EPopupGeneralType.Loading;
                    wndData.Size = EPopupGeneralSize.Small;
                }
                popup.SetDisplayData(wndData);
                CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
            }
        }

        private void LoginResponse(SComResponseAuth result, SComQueryAuth authdata)
        {

            if (result.status == 0)
            {
                CommunityLogin(result.message);
            }
            else
            {
                if (result.uuid != null)
                {
                    SComAuthUser loggedUser = new SComAuthUser();
                    loggedUser.displayName = result.displayName;
                    loggedUser.uuid = result.uuid;
                    loggedUser.username = result.username;
                    loggedUser.authenticated = true;
                    loggedUser.sessionId = result.sessionId;

                    CCommunity.saveAuthData(authdata.username, result.uuid);
                    //create profile if not exist
                    var pid = CCommunity.findMainProfile();
                    if (pid > 0)
                    {
                        loggedUser.profileCreated = pid;
                        //edit token if needed
                        var profile = CProfiles.GetProfile(pid);
                        if (profile.CommunityUUID != result.uuid)
                        {
                            CProfiles.SetCommunityProfile(pid, profile.CommunityUsername, profile.CommunityUUID);
                            CProfiles.SaveProfiles();
                        }
                    }
                    else
                    {
                        loggedUser.profileCreated = -1;

                        //download and save avatar
                        if (result.avatarUrl != null)
                        {
                            string profilePath = Path.Combine(CSettings.DataFolder, CConfig.ProfileFolders[0]);
                            string filename = null;

                            //get avatar file format (png/jpg)
                            CCommunity.checkRemoteFile(result.avatarUrl, delegate(SComDLHeaders filedata)
                            {
                                if (filedata.status == 1)
                                {
                                    if (Regex.IsMatch(filedata.contentType, "image/jpe?g", RegexOptions.IgnoreCase))
                                    {

                                        filename = CHelper.GetUniqueFileName(profilePath, "avatar.jpg", false);
                                    }
                                    else if (Regex.IsMatch(filedata.contentType, "image/png", RegexOptions.IgnoreCase))
                                    {
                                        filename = CHelper.GetUniqueFileName(profilePath, "avatar.png", false);
                                    }

                                    if (filename != null)
                                    {
                                        if (CCommunity.downloadFile(result.avatarUrl, Path.Combine(profilePath, filename)) == true)
                                        {
                                            loggedUser.avatarFile = filename;
                                            CProfiles.LoadAvatars();
                                        }
                                    }
                                }
                            });
                        }
                    }

                    CCommunity.setCurrentUser(loggedUser);
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                else
                {
                    CommunityLogin("TR_COMMUNITY_UNKERR");
                }
            }
        }
        #endregion
    }
}
