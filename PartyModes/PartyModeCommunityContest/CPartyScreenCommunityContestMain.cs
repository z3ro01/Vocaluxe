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
using System.Windows.Forms;
using VocaluxeLib.Menu;
using VocaluxeLib.Network;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.CommunityContest
{
    public class CPartyScreenCommunityContestMain : CPartyScreenCommunityContest
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private string _Themexx = "";
        private int _SelectedSongId = 0;

        public override void Init()
        {
            base.Init();
        }

        public override void LoadTheme(string xmlPath)
        {
            _Themexx = xmlPath;
            base.LoadTheme(xmlPath);
        }

        public override void ReloadTheme(string xmlPath)
        {
            base.ReloadTheme(xmlPath);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                        _NextRound();
                        break;
                    case Keys.Space:
                        selectSong();
                        break;
                }
            }


            ReloadTheme(_Themexx);

            //_PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SongID = songID;
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
           
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
          
        }

        public override bool UpdateGame()
        {
           
            return true;
        }

        private void selectSong()
        {
            int songnum = 0;
            for (int i = 0; i < CComCLib.PlayLists.Count; i++)
            {
                for (int z = 0; z < CComCLib.PlayLists[i].items.Count; z++)
                {
                    if (CComCLib.PlayLists[i].items[z].localId > 0)
                    {
                        if (songnum > 5)
                        {
                            _SelectedSongId = CComCLib.PlayLists[i].items[z].localId;
                            break;
                        }



                        songnum++;
                    }
                }
            }

            _PartyMode.GameData.Rounds[0].SongID = _SelectedSongId;
            _StartPreview(_SelectedSongId);

        }

        private void _NextRound()
        {
            _PartyMode.Next();
        }

        private void _StartPreview(int songID)
        {
            CSong song = CBase.Songs.GetSongByID(songID);
            CBase.BackgroundMusic.LoadPreview(song);
        }

        private void _EndParty()
        {
            CBase.Graphics.FadeTo(EScreen.Party);
        }
    }
}