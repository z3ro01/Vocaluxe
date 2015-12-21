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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Draw;
using VocaluxeLib.Community;

namespace Vocaluxe.Screens
{
    enum EEditMode
    {
        None,
        PlayerName,
        ServerAddr
    }

    public class CScreenProfiles : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const string _SelectSlideProfiles = "SelectSlideProfiles";
        private const string _SelectSlideDifficulty = "SelectSlideDifficulty";
        private const string _SelectSlideAvatars = "SelectSlideAvatars";
        private const string _SelectSlideUserRole = "SelectSlideUserRole";
        private const string _SelectSlideActive = "SelectSlideActive";
        private const string _SelectSlideCommunityProfile = "SelectSlideCommunityProfile";
        private const string _ButtonPlayerName = "ButtonPlayerName";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonSave = "ButtonSave";
        private const string _ButtonNew = "ButtonNew";
        private const string _ButtonDelete = "ButtonDelete";
        private const string _ButtonWebcam = "ButtonWebcam";
        private const string _ButtonSaveSnapshot = "ButtonSaveSnapshot";
        private const string _ButtonDiscardSnapshot = "ButtonDiscardSnapshot";
        private const string _ButtonTakeSnapshot = "ButtonTakeSnapshot";
        private const string _ButtonComAction = "ButtonComAction";
        private const string _TextCommunityProfile = "Text7";
        private const string _TextCommunityStatus = "Text8";

        private const string _StaticAvatar = "StaticAvatar";
        private bool _ProfilesChanged;
        private bool _AvatarsChanged;
        private bool _CommunityChanged = true;
        private string _LoadAvatarAfterChanged = null;
        private EEditMode _EditMode;

        private CTextureRef _WebcamTexture;
        private Bitmap _Snapshot;
#if DEBUG
        private string themefile;
#endif

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { _ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete, _ButtonWebcam, _ButtonSaveSnapshot, _ButtonDiscardSnapshot, _ButtonTakeSnapshot, _ButtonComAction };
            _ThemeSelectSlides = new string[] { _SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideUserRole, _SelectSlideActive };
            _ThemeStatics = new string[] { _StaticAvatar };
            _ThemeTexts = new string[] { _TextCommunityProfile, _TextCommunityStatus };
            _EditMode = EEditMode.None;
            _ProfilesChanged = false;
            _AvatarsChanged = false;
            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
            CCommunity.AddEventHandler(new EComEventType[3] { EComEventType.connectionStatus, EComEventType.loginStatus, EComEventType.settingsChanged }, (Action<SComEvent>)_OnCommunityStatusChanged);
        }

        public override void LoadTheme(string xmlPath)
        {
#if DEBUG
            themefile = xmlPath;
#endif
            base.LoadTheme(xmlPath);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();
            _SelectSlides[_SelectSlideDifficulty].SetValues<EGameDifficulty>(0);
            _SelectSlides[_SelectSlideUserRole].SetValues<EUserRole>(0);
            _SelectSlides[_SelectSlideActive].SetValues<EOffOn>(0);
            _Statics[_StaticAvatar].Aspect = EAspect.Crop;
            _Texts[_TextCommunityProfile].Visible = false;
            _Texts[_TextCommunityStatus].Visible = false;
            _Buttons[_ButtonComAction].Visible = false;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
            {
                switch (_EditMode)
                {
                    case EEditMode.None:
                        break;
                    case EEditMode.PlayerName:
                        _SelectSlides[_SelectSlideProfiles].RenameValue(
                            CProfiles.AddGetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag, keyEvent.Unicode));
                        _ProfilesChanged = true;
                        break;
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
#if DEBUG
                    case Keys.Space:
                        ReloadTheme(themefile);
                        break;
#endif
                    case Keys.Escape:
                        if (_EditMode == EEditMode.PlayerName)
                            _EditMode = EEditMode.None;
                        else
                            CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreen.Main);
                        else if (_Buttons[_ButtonSave].Selected)
                            _SaveProfiles();
                        else if (_Buttons[_ButtonNew].Selected)
                            _NewProfile();
                        else if (_Buttons[_ButtonPlayerName].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        }
                        else if (_Buttons[_ButtonDelete].Selected)
                            _DeleteProfile();
                        else if (_Buttons[_ButtonWebcam].Selected)
                            _OnWebcam();
                        else if (_Buttons[_ButtonSaveSnapshot].Selected)
                            _OnSaveSnapshot();
                        else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                            _OnDiscardSnapshot();
                        else if (_Buttons[_ButtonTakeSnapshot].Selected)
                            _OnTakeSnapshot();
                        else if (_Buttons[_ButtonComAction].Selected)
                            _OnCommunityButtonPressed();
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            _SelectSlides[_SelectSlideProfiles].RenameValue(
                                CProfiles.GetDeleteCharInPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag));
                            _ProfilesChanged = true;
                        }
                        else
                            CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.Delete:
                        _DeleteProfile();
                        break;
                }
                if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreen.Main);
                else if (_Buttons[_ButtonSave].Selected)
                    _SaveProfiles();
                else if (_Buttons[_ButtonNew].Selected)
                    _NewProfile();
                else if (_Buttons[_ButtonDelete].Selected)
                    _DeleteProfile();
                else if (_Buttons[_ButtonPlayerName].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                }
                else if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                    if (CWebcam.IsDeviceAvailable() && _WebcamTexture != null)
                        _OnDiscardSnapshot();
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_SelectSlides[_SelectSlideProfiles].SelectedTag,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
                else if (_Buttons[_ButtonWebcam].Selected)
                    _OnWebcam();
                else if (_Buttons[_ButtonSaveSnapshot].Selected)
                    _OnSaveSnapshot();
                else if (_Buttons[_ButtonDiscardSnapshot].Selected)
                    _OnDiscardSnapshot();
                else if (_Buttons[_ButtonTakeSnapshot].Selected)
                    _OnTakeSnapshot();
                else if (_Buttons[_ButtonComAction].Selected)
                    _OnCommunityButtonPressed();
                
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreen.Main);
            return true;
        }

        public override bool UpdateGame()
        {
            if (_AvatarsChanged)
                _LoadAvatars(true);

            if (_ProfilesChanged)
                _LoadProfiles(true);

            if (_SelectSlides[_SelectSlideProfiles].Selection > -1)
            {
                _Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                if (_EditMode == EEditMode.PlayerName)
                    _Buttons[_ButtonPlayerName].Text.Text += "|";

                _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                
                int avatarID = CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                _SelectSlides[_SelectSlideAvatars].SelectedTag = avatarID;
                if (_Snapshot == null)
                {
                    if (CWebcam.IsCapturing())
                    {
                        if (CWebcam.GetFrame(ref _WebcamTexture))
                            _Statics[_StaticAvatar].Texture = _WebcamTexture;
                    }
                    else
                        _Statics[_StaticAvatar].Texture = CProfiles.GetAvatarTexture(avatarID);
                }
            }

            if (_CommunityChanged)
                _DisplayCommunity();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _LoadAvatars(false);
            _LoadProfiles(false);
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();
            _EditMode = EEditMode.None;
            _OnDiscardSnapshot();
        }

        #region Community
        private void _DisplayCommunity()
        {
            if (CCommunity.isEnabled())
            {
                _Texts[_TextCommunityProfile].Visible = true;
                _Texts[_TextCommunityStatus].Visible = true;
                _Buttons[_ButtonComAction].Visible = true;

                var selectedProfile = CProfiles.GetProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                if (selectedProfile != null) { 
                    if (!String.IsNullOrWhiteSpace(selectedProfile.CommunityUsername) && !String.IsNullOrWhiteSpace(selectedProfile.CommunityUUID))
                    {
                        _Texts[_TextCommunityStatus].Text = selectedProfile.CommunityUsername;
                    }
                    else
                    {
                        _Texts[_TextCommunityStatus].Text = "Log in to activate";
                    }
                }
            }
            else
            {
                _Texts[_TextCommunityProfile].Visible = false;
                _Texts[_TextCommunityStatus].Visible = false;
                _Buttons[_ButtonComAction].Visible = false;
            }
        }

        private void _OnCommunityButtonPressed()
        {
            if (CCommunity.isEnabled())
            {
                var selectedProfile = CProfiles.GetProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                if (selectedProfile != null) { 
                    if (!String.IsNullOrWhiteSpace(selectedProfile.CommunityUsername) && !String.IsNullOrWhiteSpace(selectedProfile.CommunityUUID))
                    {
                        if (selectedProfile.CommunityUsername == CCommunity.config.AuthUser && selectedProfile.CommunityUUID == CCommunity.config.AuthUUID)
                        {
                            CPopupHelper.Alert("TR_COMMUNITY_PROFILE", "TR_COMMUNITY_MAINPROFILEISVALID");
                        }
                        else { 
                            //validating 
                            CPopupHelper.Loading("TR_COMMUNITY_LOADING", "TR_COMMUNITY_VALIDATINGPROFILE");

                            SComQueryAuth data = new SComQueryAuth();
                            data.username = selectedProfile.CommunityUsername;
                            data.password = selectedProfile.CommunityUUID;
                            data.method = "validate-uuid";

                            var currUser = CCommunity.getCurrentUser();
                            data.parameters = new Dictionary<string, string>();
                            data.parameters.Add("vcxMainUser", CCommunity.config.AuthUser);
                            data.parameters.Add("vcxMainUUID", CCommunity.config.AuthUUID);

                            CCommunity.authWithUUIDAsync(data, (Action<SComResponseAuth>)_ValidateResponse);
                        }
                    }
                    else
                    {
                       //open login window
                       CPopupHelper.Login(CLanguage.Translate("TR_COMMUNITY_TXTLOGIN").Replace("%s", CCommunity.getName()), "", (Action<SPopupGeneralEvent>)_LoginWindowCallback);
                    }
                }
            }
        }

        private void _LoginWindowCallback(SPopupGeneralEvent eventData)
        {
           if (eventData.Name.Equals("onKeyEscape")
              || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
              || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo")))
            {
                CGraphics.HidePopup(EPopupScreens.PopupGeneral);
            }
           else if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes")))
            {
                var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
                var wndData = popup.GetDisplayData();

                SComQueryAuth auth = new SComQueryAuth();
                if (wndData.Username == null || wndData.Username.Length == 0 || wndData.Password == null || wndData.Password.Length == 0)
                {
                    wndData.TextMessage = CLanguage.Translate("TR_COMMUNITY_LOGINREQFIELDS");
                }
                else
                {
                    var currUser    = CCommunity.getCurrentUser();
                    auth.username   = wndData.Username;
                    auth.password   = wndData.Password;
                    auth.method     = "auth-roaming";
                    auth.parameters = new Dictionary<string,string>();
                    auth.parameters.Add("vcxMainUser", CCommunity.config.AuthUser);
                    auth.parameters.Add("vcxMainUUID", CCommunity.config.AuthUUID);

                    CCommunity.authRequestAsync(auth, (Action<SComResponseAuth, SComQueryAuth>) _LoginResponse);

                    wndData.TextMessage = CLanguage.Translate("TR_COMMUNITY_LOADING");
                    wndData.Type = EPopupGeneralType.Loading;
                    wndData.Size = EPopupGeneralSize.Small;
                }
                popup.SetDisplayData(wndData);
                CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
            }
        }

        private void _ValidateResponse(SComResponseAuth result)
        {
            var pid = _SelectSlides[_SelectSlideProfiles].SelectedTag;
            if (result.status == 0)
            {
                //not valid
                if (result.code == 1)
                {
                    var cp = CProfiles.GetCommunityProfile(pid);
                    CProfiles.SetCommunityProfile(pid, cp.username, null);
                    CPopupHelper.Alert("TR_COMMUNITY_PROFILE", result.message != null ? result.message : "TR_COMMUNITY_PROFILENOTVALID");
                }
                else
                {
                    CPopupHelper.Alert("TR_COMMUNITY_PROFILE", result.message != null ? result.message : "TR_COMMUNITY_UNKERR");
                }
            }
            else if (result.status == 1)
            {
                CPopupHelper.Alert("TR_COMMUNITY_PROFILE", "TR_COMMUNITY_PROFILEVALID");
            }
            else
            {
                CPopupHelper.Alert("TR_COMMUNITY_PROFILE", result.message != null?result.message:"TR_COMMUNITY_UNKERR");
            }
        }

        private void _LoginResponse(SComResponseAuth result, SComQueryAuth authdata)
        {
            if (result.status == 0)
            {
                CPopupHelper.Login(CLanguage.Translate("TR_COMMUNITY_TXTLOGIN").Replace("%s", CCommunity.getName()), result.message, (Action<SPopupGeneralEvent>)_LoginWindowCallback);
            }
            else
            {
                if (result.uuid != null)
                {
                    var pid = _SelectSlides[_SelectSlideProfiles].SelectedTag;

                    if (result.displayName != null)
                        CProfiles.SetPlayerName(pid, result.displayName);
                    CProfiles.SetDifficulty(pid, EGameDifficulty.TR_CONFIG_NORMAL);
                    CProfiles.SetActive(pid, EOffOn.TR_CONFIG_ON);
                    CProfiles.SetCommunityProfile(pid, result.username, result.uuid);
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
                                        CProfiles.LoadAvatars();
                                        _LoadAvatarAfterChanged = filename;

                                       /* var avatar = CProfiles.GetAvatarByFilename(filename);
                                        if (avatar != null) {
                                            _Statics[_StaticAvatar].Texture = avatar.Texture;
                                            CProfiles.SetAvatar(pid, avatar.ID);
                                            _SelectSlides[_SelectSlideAvatars].SelectedTag = avatar.ID;
                                        }*/
                                    }
                                }
                            }
                        });
                    }

                    _ProfilesChanged = true;
                    CProfiles.SaveProfiles();
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                else
                {
                    CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_UNKERR");
                }
            }
        }

        private void _OnCommunityStatusChanged(SComEvent evdata)
        {
            Console.WriteLine(evdata.eventType.ToString());

            _CommunityChanged = true;
        }

        #endregion

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Avatar == (EProfileChangedFlags.Avatar & flags))
                _AvatarsChanged = true;

            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
                _ProfilesChanged = true;
        }

        private void _OnTakeSnapshot()
        {
            if (!CWebcam.IsDeviceAvailable())
            {
                CDraw.RemoveTexture(ref _WebcamTexture);
                _Snapshot = null;
                _Buttons[_ButtonSaveSnapshot].Visible = false;
                _Buttons[_ButtonDiscardSnapshot].Visible = false;
                _Buttons[_ButtonTakeSnapshot].Visible = false;
                _Buttons[_ButtonWebcam].Visible = false;
            }
            else
            {
                CWebcam.Stop(); //Do this first to get consistent frame and bitmap
                _Snapshot = CWebcam.GetBitmap();
                if (CWebcam.GetFrame(ref _WebcamTexture))
                    _Statics[_StaticAvatar].Texture = _WebcamTexture;
                _Buttons[_ButtonSaveSnapshot].Visible = true;
                _Buttons[_ButtonDiscardSnapshot].Visible = true;
                _Buttons[_ButtonTakeSnapshot].Visible = false;
                _Buttons[_ButtonWebcam].Visible = false;
            }
        }

        private void _OnDiscardSnapshot()
        {
            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();
        }

        private void _OnSaveSnapshot()
        {
            string file = CHelper.GetUniqueFileName(Path.Combine(CSettings.DataFolder, CConfig.ProfileFolders[0]), "snapshot.png");
            _Snapshot.Save(file, ImageFormat.Png);

            _Snapshot = null;
            CDraw.RemoveTexture(ref _WebcamTexture);

            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = false;
            _Buttons[_ButtonWebcam].Visible = CWebcam.IsDeviceAvailable();

            int id = CProfiles.NewAvatar(file);
            CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag, id);
            _LoadAvatars(false);
        }

        private void _OnWebcam()
        {
            if (!CWebcam.IsDeviceAvailable())
            {
                _Buttons[_ButtonWebcam].Visible = false;
                return;
            }
            _Snapshot = null;
            CWebcam.Start();
            _Buttons[_ButtonSaveSnapshot].Visible = false;
            _Buttons[_ButtonDiscardSnapshot].Visible = false;
            _Buttons[_ButtonTakeSnapshot].Visible = true;
            _Buttons[_ButtonWebcam].Visible = false;
        }

        private void _NewProfile()
        {
            _EditMode = EEditMode.None;
            int id = CProfiles.NewProfile();
            _LoadProfiles(false);
            _SelectSlides[_SelectSlideProfiles].SelectedTag = id;

            CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag, _SelectSlides[_SelectSlideAvatars].SelectedTag);

            _SelectElement(_Buttons[_ButtonPlayerName]);
            _EditMode = EEditMode.PlayerName;
        }

        private void _SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
        }

        private void _DeleteProfile()
        {
            _EditMode = EEditMode.None;

            if (CCommunity.isEnabled()) { 
                var selectedProfile = CProfiles.GetProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag);
                if (selectedProfile != null)
                {
                    if (!String.IsNullOrWhiteSpace(selectedProfile.CommunityUsername) && !String.IsNullOrWhiteSpace(selectedProfile.CommunityUUID))
                    {
                        if (selectedProfile.CommunityUsername == CCommunity.config.AuthUser && selectedProfile.CommunityUUID == CCommunity.config.AuthUUID)
                        {
                            CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_MAINPROFILENOTDELETEABLE");
                            return;
                        }
                    }
                }
            }

            CProfiles.DeleteProfile(_SelectSlides[_SelectSlideProfiles].SelectedTag);

            int selection = _SelectSlides[_SelectSlideProfiles].Selection;
            if (_SelectSlides[_SelectSlideProfiles].NumValues - 1 > selection)
                _SelectSlides[_SelectSlideProfiles].Selection = selection + 1;
            else
                _SelectSlides[_SelectSlideProfiles].Selection = selection - 1;
        }

        private void _LoadProfiles(bool keep)
        {
            string name = String.Empty;
            if (_EditMode == EEditMode.PlayerName)
                name = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag);

            int selectedProfileID = _SelectSlides[_SelectSlideProfiles].SelectedTag;
            _SelectSlides[_SelectSlideProfiles].Clear();

            CProfile[] profiles = CProfiles.GetProfiles();
            foreach (CProfile profile in profiles)
                _SelectSlides[_SelectSlideProfiles].AddValue(profile.PlayerName, null, profile.ID);

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                if (selectedProfileID != -1)
                    _SelectSlides[_SelectSlideProfiles].SelectedTag = selectedProfileID;
                else
                {
                    _SelectSlides[_SelectSlideProfiles].Selection = 0;
                    selectedProfileID = _SelectSlides[_SelectSlideProfiles].SelectedTag;
                }

                if (!keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileID);
                    _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(selectedProfileID);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileID);
                    _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(selectedProfileID);
                }

                if (_EditMode == EEditMode.PlayerName)
                    CProfiles.SetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag, name);
            }
            _ProfilesChanged = false;
        }

        private void _LoadAvatars(bool keep)
        {
            int selectedAvatarID = _SelectSlides[_SelectSlideAvatars].SelectedTag;
            _SelectSlides[_SelectSlideAvatars].Clear();
            IEnumerable<CAvatar> avatars = CProfiles.GetAvatars();
            if (avatars != null)
            {
                foreach (CAvatar avatar in avatars)
                    _SelectSlides[_SelectSlideAvatars].AddValue(avatar.GetDisplayName(), null, avatar.ID);
            }

            if (keep)
            {
                _SelectSlides[_SelectSlideAvatars].SelectedTag = selectedAvatarID;
                CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag, selectedAvatarID);
            }
            else
                _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].SelectedTag);

            if (_LoadAvatarAfterChanged != null)
            {
                var avatar = CProfiles.GetAvatarByFilename(_LoadAvatarAfterChanged);
                if (avatar != null)
                {
                    _Statics[_StaticAvatar].Texture = avatar.Texture;
                    CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag, avatar.ID);
                }
                _LoadAvatarAfterChanged = null;
            }
            _AvatarsChanged = false;
        }
    }
}