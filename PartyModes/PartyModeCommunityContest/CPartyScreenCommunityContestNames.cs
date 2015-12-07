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

using System.Collections.Generic;
using System;
using VocaluxeLib.Menu;
using VocaluxeLib.Network;

namespace VocaluxeLib.PartyModes.CommunityContest
{
    public class CPartyScreenCommunityContestNames : CMenuPartyNameSelection
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeCommunityContest _PartyMode;

        public override void Init()
        {
            base.Init();

            _PartyMode = (CPartyModeCommunityContest)base._PartyMode;
            _AllowChangePlayerNum = true;
        }

        public override void OnShow()
        {
            base.OnShow();
         
            if (_PartyMode.GameData.ProfileIDs.Count > 0) {
                Console.WriteLine("Have profile");
                List<int>[] ids = new List<int>[1] { _PartyMode.GameData.ProfileIDs };
                SetPartyModeData(_PartyMode.GameData.ProfileIDs.Count);
                SetPartyModeProfiles(ids);
            }
            else
            {
                //select default profile
                List<int>[] ids = new List<int>[1];
                var profiles = CBase.Profiles.GetProfiles();
                if (profiles != null && profiles.Length > 0) {
                    for (var i = 0; i < profiles.Length; i++)
                    {
                        if (!String.IsNullOrEmpty(profiles[i].CommunityProfile) && profiles[i].CommunityProfile.Equals(CCommunity.getAuthProfileFileName()))
                        {
                            if (ids[0] == null) { 
                                ids[0] = new List<int>();
                                ids[0].Add(profiles[i].ID);
                            }
                        }
                    }
                }

                if (ids.Length > 0 && ids[0] != null) {
                    SetPartyModeData(ids[0].Count);
                    SetPartyModeProfiles(ids);
                }
                else
                {
                    SetPartyModeData(1);
                }
            }
        }

        public override void Back()
        {
            if (_TeamList != null && _TeamList.Length == 1)
                _PartyMode.GameData.ProfileIDs = _TeamList[0];
            _PartyMode.Back();
        }

        private string GetNetworkProfile(int profileID)
        {
            var profiles = CBase.Profiles.GetProfiles();
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i].ID == profileID)
                    if (!String.IsNullOrEmpty(profiles[i].CommunityProfile))
                        return profiles[i].CommunityProfile;
            }
            return String.Empty;
        }

        public override void Next()
        {
            _PartyMode.GameData.ProfileIDs.Clear();

           //check profiles
            showLoadingPopup("Felhasználók ellenőrzése ...");
            if (_TeamList.Length == 1)
            {
                bool error = false;
                string[] profileFiles = new string[_TeamList[0].Count];
                for (int i = 0; i < _TeamList[0].Count; i++)
                {
                    string nprofile = GetNetworkProfile(_TeamList[0][i]);
                    if (String.IsNullOrEmpty(nprofile) || String.IsNullOrWhiteSpace(nprofile))
                    {
                        error = true;
                        showAlertPopup("Error", "No Way!");
                    }
                    else
                    {
                        profileFiles[i] = nprofile;
                        _PartyMode.GameData.ProfileIDs.Add(_TeamList[0][i]);
                    }
                }

                if (!error)
                {
                    CCommunity.getContestAccessAsync(profileFiles, _PartyMode.GameData.SelectedContestId, delegate(SComResponse response)
                    {
                        if (response.status == 1)
                        {
                            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                            _PartyMode.Next();
                        }
                        else
                        {
                            showAlertPopup("Error", response.message);
                        }
                    });
                }
            }
        }

        private void showLoadingPopup(string title)
        {
            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyEscape", delegate(SPopupGeneralEvent eventData)
            {
                CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            });

            data.type = EPopupGeneralType.Loading;
            data.size = EPopupGeneralSize.Small;
            data.TextTitle = title;

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private void showAlertPopup(string title, string message)
        {
            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyBack,onKeyEscape,onMouseLB", delegate(SPopupGeneralEvent eventData)
            {
                CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            });

            data.type = EPopupGeneralType.Alert;
            data.size = EPopupGeneralSize.Medium;
            data.TextTitle = title;
            data.TextMessage = message;
            data.ButtonOkLabel = "TR_BUTTON_OK";

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }
    }
}