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
    public class CScreenMain : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _ButtonSing = "ButtonSing";
        private const string _ButtonParty = "ButtonParty";
        private const string _ButtonOptions = "ButtonOptions";
        private const string _ButtonProfiles = "ButtonProfiles";
        private const string _ButtonExit = "ButtonExit";
        private const string _StaticWarningProfiles = "StaticWarningProfiles";
        private const string _TextWarningProfiles = "TextWarningProfiles";
        private const string _TextRelease = "TextRelease";
        #if DEBUG
            private string themefile;
        #endif
        private const string _ButtonLogin = "ButtonLogin";
        private const string _LoginAvatar = "StaticLoginAvatar";
        private const string _StaticSongsArea = "CommunityNewSongsArea";
        private const string _StaticComMenu = "StaticComMenu";
        private const string _StaticCSongBg = "CommunityNewSongsTitle";
        private const string _TextCSongTitle = "TextCSongTitle";
        private const string _TextCNewSongs = "TextCNewSongs";
        private const string _TextCEvents   = "TextCEvents";
        private const string _TextCLoading = "TextCLoading";
        private const string _StaticLoading = "CommunityLoading";


        private CAvatar _Avatar;
        private SPopupGeneral wndData;
        private Boolean CComUpdated = false;
        private CComMainScreen newsArea;
        private Boolean _newsAreaVisible = false;
        private Boolean _ThemeLoaded = false;

        //CParticleEffect Snowflakes;
        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] { "StaticMenuBar", _StaticWarningProfiles, _LoginAvatar, _StaticComMenu, _StaticSongsArea, _StaticCSongBg, _StaticLoading };
            _ThemeButtons = new string[] { _ButtonLogin, _ButtonSing, _ButtonParty, _ButtonOptions, _ButtonProfiles, _ButtonExit };
            _ThemeTexts = new string[] { _TextRelease, _TextWarningProfiles, _TextCSongTitle, _TextCNewSongs, _TextCEvents, _TextCLoading };
            CommunityInit();
        }

        public override void LoadTheme(string xmlPath)
        {
            #if DEBUG
                themefile = xmlPath;
            #endif
            base.LoadTheme(xmlPath);

            _Texts[_TextRelease].Text = CSettings.GetFullVersionText();
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            _Texts[_TextRelease].Visible = CSettings.VersionRevision != ERevision.Release;
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            _Statics[_StaticWarningProfiles].Visible = false;
            _Texts[_TextWarningProfiles].Visible = false;
            _Texts[_TextCEvents].Visible = false;
            _Texts[_TextCNewSongs].Visible = false;
            _Statics[_StaticCSongBg].Visible = false;

            _ThemeLoaded = true;
            if (!CCommunity.isEnabled()) { 
                _Statics[_StaticLoading].Visible = false;
                _Texts[_TextCLoading].Visible = false;
            }
            newsArea = new CComMainScreen(_Statics[_StaticSongsArea], _Texts[_TextCSongTitle]);
        }
        
        public override void Draw()
        {
            base.Draw();
            if (_newsAreaVisible)
                newsArea.Draw();
        }

        public override void OnShow()
        {
            CommunityDisplay();
            base.OnShow();
            newsArea.OnShow();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    #if DEBUG
                    case Keys.Space:
                        ReloadTheme(themefile);
                        newsArea.ReloadTheme(themefile);
                        break;
                    #endif
                    case Keys.O:
                        CGraphics.FadeTo(EScreen.Options);
                        break;

                    case Keys.S:
                        if (CProfiles.NumProfiles > 0)
                            CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.C:
                        CGraphics.FadeTo(EScreen.Credits);
                        break;

                    case Keys.T:
                        CGraphics.FadeTo(EScreen.Test);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonSing].Selected)
                        {
                            CParty.SetNormalGameMode();
                            CGraphics.FadeTo(EScreen.Song);
                        }

                        if (_Buttons[_ButtonParty].Selected)
                            CGraphics.FadeTo(EScreen.Party);

                        if (_Buttons[_ButtonOptions].Selected)
                            CGraphics.FadeTo(EScreen.Options);

                        if (_Buttons[_ButtonProfiles].Selected)
                            CGraphics.FadeTo(EScreen.Profiles);

                        if (_Buttons[_ButtonExit].Selected)
                            return false;

                        if (_Buttons[_ButtonLogin].Selected)
                            CommunityLogin();

                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (CCommunity.isEnabled()) { 
                if (newsArea.HandleMouse(mouseEvent) == true) 
                    return true;
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonSing].Selected)
                {
                    CParty.SetNormalGameMode();
                    CGraphics.FadeTo(EScreen.Song);
                }

                if (_Buttons[_ButtonParty].Selected)
                    CGraphics.FadeTo(EScreen.Party);

                if (_Buttons[_ButtonOptions].Selected)
                    CGraphics.FadeTo(EScreen.Options);

                if (_Buttons[_ButtonProfiles].Selected)
                    CGraphics.FadeTo(EScreen.Profiles);

                if (_Buttons[_ButtonExit].Selected)
                    return false;

                if (_Buttons[_ButtonLogin].Selected)
                    CommunityLogin();
            }

            return true;
        }

        public override bool UpdateGame()
        {
            bool profileOK = CProfiles.NumProfiles > 0;
            _Statics[_StaticWarningProfiles].Visible = !profileOK;
            _Texts[_TextWarningProfiles].Visible = !profileOK;
            _Buttons[_ButtonSing].Selectable = profileOK;
            _Buttons[_ButtonParty].Selectable = profileOK;

            if (CComUpdated)
            {
                CommunityDisplay();
                CComUpdated = false;
            }

            if (_Statics[_StaticLoading].Visible == true)
            {
                _Statics[_StaticLoading].Alpha = _Statics[_StaticLoading].Alpha == 1 ? _Statics[_StaticLoading].Alpha = 0 : _Statics[_StaticLoading].Alpha = 1;
            }

            return true;
        }

        #region Community

        private void CommunityInit()
        {
            CCommunity.AddEventHandler((Action<SComEvent>)_OnCommunityEvent);
            CCommunity.Init();
            CCommunity.checkConnection();
            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
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
            //Community status changed (login or connection)

            Console.WriteLine(eventData.eventType.ToString());

            CComUpdated = true;
            if (eventData.eventType == EComEventType.loginStatus && eventData.status == 1) { 
                newsArea.OnLoggedIn();
                ShowNewsArea();
            }

            if (eventData.eventType == EComEventType.loginStatus && eventData.status == 0)
            {
                HideNewsArea();
            }

            if (eventData.eventType == EComEventType.loading && eventData.status == 1)
            {
                if (_ThemeLoaded) { 
                    _Statics[_StaticLoading].Visible = true;
                    _Texts[_TextCLoading].Visible = true;
                    _Statics[_StaticLoading].Alpha = 1;
                }
            }
            else
            {
                 if (_ThemeLoaded) { 
                     _Statics[_StaticLoading].Visible = false;
                    _Texts[_TextCLoading].Visible = false;
                 }
            }
            
        }

        public void CommunityDisplay()
        {
            _Statics[_LoginAvatar].Alpha = 1;
            _Buttons[_ButtonLogin].Text.Alpha = 1;

            //load default avatar
            if (CCommunity.isEnabled() && _Avatar != null)
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
                        ShowNewsArea();
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
                else
                {
                    _Buttons[_ButtonLogin].Text.Text = CLanguage.Translate("TR_SCREENMAIN_TEXT_CLOGIN");
                    _Statics[_LoginAvatar].Visible = true;
                    _Buttons[_ButtonLogin].Visible = true;
                    if (_Avatar != null)
                    {
                        _Statics[_LoginAvatar].Texture = _Avatar.Texture;
                    }
                }
            }
            else
            {
                _Statics[_LoginAvatar].Visible = false;
                _Buttons[_ButtonLogin].Visible = false;
                _Statics[_StaticComMenu].Visible = false;
                HideNewsArea();
            }
        }

        private void CommunityLogin(String emsg = null)
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            SPopupGeneral data = new SPopupGeneral();

            if (CCommunity.isReadyForAuth())
            {
                if (CCommunity.connectionStatus == true) {
                    CPopupHelper.Confirm("TR_COMMUNITY_TXTLOGOUT", CLanguage.Translate("TR_COMMUNITY_TXTLOGOUT_CONFIRM").Replace("\\n","\n").Replace("%s",CCommunity.getName()), LogoutWindowCallback);
                }
                else
                {
                    data.Type = EPopupGeneralType.Alert;
                    data.Size = EPopupGeneralSize.Small;
                    data.TextTitle = CLanguage.Translate("TR_COMMUNITY_OFFLINE");
                    data.TextMessage = CLanguage.Translate("TR_COMMUNITY_OFFLINEALERT");
                    data.ButtonOkLabel = CLanguage.Translate("TR_COMMUNITY_BTNOK");
                    popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", (Action<SPopupGeneralEvent>)LogoutWindowCallback);
                    popup.SetDisplayData(data);
                    CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
                }
            }
            else {
                //create login window
                popup.AddEventHandler("onKeyReturn,onKeyEscape,onMouseLB", (Action<SPopupGeneralEvent>)LoginWindowCallback);
                data.TextTitle = CLanguage.Translate("TR_COMMUNITY_TXTLOGIN").Replace("%s", CCommunity.getName());
                data.TextMessage = "";
                if (emsg != null) { data.TextMessage = emsg; }
                data.Username = wndData.Username;
                data.Type = EPopupGeneralType.Login;
                data.Size = EPopupGeneralSize.Medium;
                data.ButtonNoLabel  = CLanguage.Translate("TR_COMMUNITY_BTNCANCEL");
                data.ButtonYesLabel = CLanguage.Translate("TR_COMMUNITY_BTNLOGIN");
                popup.SetDisplayData(data);
                CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
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
                wndData = popup.GetDisplayData();
               
                SComQueryAuth auth = new SComQueryAuth();
                if (wndData.Username == null || wndData.Username.Length == 0 || wndData.Password == null || wndData.Password.Length == 0)
                {
                    wndData.TextMessage = CLanguage.Translate("TR_COMMUNITY_LOGINREQFIELDS");
                }
                else {
                    auth.username = wndData.Username;
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

        private void ShowNewsArea()
        {
            _newsAreaVisible = true;
            _Texts[_TextCSongTitle].Visible = true;
            _Texts[_TextCEvents].Visible = true;
            _Texts[_TextCNewSongs].Visible = true;
            _Statics[_StaticCSongBg].Visible = true;
        }

        private void HideNewsArea()
        {
            _newsAreaVisible = false;
            _Texts[_TextCSongTitle].Visible = false;
            _Texts[_TextCEvents].Visible = false;
            _Texts[_TextCNewSongs].Visible = false;
            _Statics[_StaticCSongBg].Visible = false;
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
                    else {
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
                                    if (Regex.IsMatch(filedata.contentType, "image/jpe?g", RegexOptions.IgnoreCase)) {

                                        filename = CHelper.GetUniqueFileName(profilePath, "avatar.jpg", false);
                                    }
                                    else if (Regex.IsMatch(filedata.contentType, "image/png", RegexOptions.IgnoreCase)){
                                        filename = CHelper.GetUniqueFileName(profilePath, "avatar.png", false);
                                    }
                                   
                                    if (filename != null) {
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
                    CommunityLogin(CLanguage.Translate("TR_COMMUNITY_UNKERR"));
                }
            }
        }
        #endregion
    }
}