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
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuPartyGameModeSelection : CMenuParty
    {
        protected List<EGameMode> _Whitelist;
        protected List<EGameMode> _Blacklist;
        protected List<EGameMode> _SelectedModes;

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";
        private const string _SelectSlideGameMode = "SelectSlideGameMode";

        private int _CurrentSelection = -1;

        public override void Init()
        {
            base.Init();
            _Whitelist = new List<EGameMode>();
            _Blacklist = new List<EGameMode>();
            _SelectedModes = new List<EGameMode>();

            _SelectedModes.Add(EGameMode.TR_GAMEMODE_BLIND);

            _ThemeButtons = new string[] { _ButtonBack, _ButtonNext };
            _ThemeSelectSlides = new string[] { _SelectSlideGameMode };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideGameMode].AddValue("TR_GAMEMODE_ALL");
            _SelectSlides[_SelectSlideGameMode].AddValues(Enum.GetNames(typeof(EGameMode)));
            _SelectSlides[_SelectSlideGameMode].AddValue("TR_GAMEMODE_CUSTOM");
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            switch (keyEvent.Key)
            {
                case Keys.Back:
                case Keys.Escape:
                    Back();
                    break;

                case Keys.Enter:
                    if (_Buttons[_ButtonBack].Selected)
                        Back();

                    if (_Buttons[_ButtonNext].Selected)
                        Next();

                    break;
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    Back();

                if (_Buttons[_ButtonNext].Selected)
                    Next();
            }
            if (mouseEvent.RB)
                Back();

            return true;
        }

        public abstract void Back();

        public abstract void Next();

        public override bool UpdateGame()
        {
            if(_SelectSlides[_SelectSlideGameMode].Selection != _CurrentSelection)
            {
                _CurrentSelection = _SelectSlides[_SelectSlideGameMode].Selection;
                _SelectedModes.Clear();
                if(_CurrentSelection == 0)
                {
                    if (_Whitelist.Count > 0)
                        _SelectedModes.AddRange(_Whitelist);
                    else
                    {
                        foreach(EGameMode gm in Enum.GetValues(typeof(EGameMode)))
                        {
                            if (!_Blacklist.Contains(gm))
                                _SelectedModes.Add(gm);
                        }
                    }
                }
                else if(_CurrentSelection == _SelectSlides[_SelectSlideGameMode].NumValues - 1)
                {
                    //TODO
                }
                else
                {
                    if (_Whitelist.Count > 0 && _Whitelist.Count < _CurrentSelection - 1)
                        _SelectedModes.Add(_Whitelist[_CurrentSelection - 1]);
                    else if (_Blacklist.Count > 0)
                    {

                    }
                    else
                    {
                        _SelectedModes.Add((EGameMode)Enum.GetValues(typeof(EGameMode)).GetValue(_CurrentSelection - 1));
                    }
                        

                }
            }
            return true;
        }
    }
}