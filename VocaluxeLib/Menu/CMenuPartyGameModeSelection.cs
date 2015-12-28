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
    public struct SPartyGameMode
    {
        public EGameMode GameMode;
        public string GameModeName;
        public bool IsPartyGameMode
        {
            get
            {
                return !string.IsNullOrWhiteSpace(GameModeName);
            }
        }
    }

    public abstract class CMenuPartyGameModeSelection : CMenuParty
    {
        //Add elements to this lists to distinct available game-modes
        protected List<EGameMode> _Whitelist;
        protected List<EGameMode> _Blacklist;
        //Add strings to this list to provide own party-mode-special game-mides
        protected List<SPartyGameMode> _OwnGameModes;
        //This list will contain all selected game-modes
        protected List<SPartyGameMode> _SelectedModes;

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";
        private const string _SelectSlideGameMode = "SelectSlideGameMode";
        private List<string> _SelectSlideGameModes;
        private List<string> _TextGameMode;
        private List<string> _TextGameModeDesc;

        private int _CurrentSelection = -1;

        public override void Init()
        {
            base.Init();
            _Whitelist = new List<EGameMode>();
            _Blacklist = new List<EGameMode>();
            _OwnGameModes = new List<SPartyGameMode>();
            _SelectedModes = new List<SPartyGameMode>();

            _SelectSlideGameModes = new List<string>();
            _TextGameMode = new List<string>();
            _TextGameModeDesc = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                _SelectSlideGameModes.Add("SelectSlideGameMode" + (i + 1));
                _TextGameMode.Add("TextGameMode" + (i + 1));
                _TextGameModeDesc.Add("TextGameModeDesc" + (i + 1));
            }

            List<string> themeHelper = new List<string>();
            themeHelper.Add(_SelectSlideGameMode);
            themeHelper.AddRange(_SelectSlideGameModes);

            _ThemeButtons = new string[] { _ButtonBack, _ButtonNext };
            _ThemeSelectSlides = themeHelper.ToArray();

            themeHelper.Clear();
            themeHelper.AddRange(_TextGameMode);
            themeHelper.AddRange(_TextGameModeDesc);

            _ThemeTexts = themeHelper.ToArray();

        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideGameMode].AddValue("TR_GAMEMODE_ALL");
            _SelectSlides[_SelectSlideGameMode].AddValues(Enum.GetNames(typeof(EGameMode)));
            foreach (SPartyGameMode pgm in _OwnGameModes)
                _SelectSlides[_SelectSlideGameMode].AddValue(pgm.GameModeName);
            _SelectSlides[_SelectSlideGameMode].AddValue("TR_GAMEMODE_CUSTOM");

            for(int i=0; i< _SelectSlideGameModes.Count; i++)
            {
                _SelectSlides[_SelectSlideGameModes[i]].SetValues<EOffOn>((int)EOffOn.TR_CONFIG_ON);
                _Texts[_TextGameMode[i]].Text = "Test";
                _Texts[_TextGameModeDesc[i]].Text = "Dies ist eine Beschreibung des entsprechenden Spiel-Modus, damit alle wissen was da so abgeht";
            }
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
            //Only update _SelectedModes if something has changed
            if(_SelectSlides[_SelectSlideGameMode].Selection != _CurrentSelection)
            {
                _CurrentSelection = _SelectSlides[_SelectSlideGameMode].Selection;
                _SelectedModes.Clear();

                //First value: Select all
                if(_CurrentSelection == 0)
                {
                    //Elements in whitelist: Only use these ones and party-based game-mides
                    if (_Whitelist.Count > 0)
                    {
                        foreach(EGameMode gm in _Whitelist)
                        {
                            SPartyGameMode pgm = new SPartyGameMode();
                            pgm.GameMode = gm;

                            _SelectedModes.Add(pgm);
                        }
                        _SelectedModes.AddRange(_OwnGameModes);
                    }
                    //Otherwise: Check if gm is on blacklist before adding
                    else
                    {
                        foreach (EGameMode gm in Enum.GetValues(typeof(EGameMode)))
                        {
                            if (!_Blacklist.Contains(gm))
                            {
                                SPartyGameMode pgm = new SPartyGameMode();
                                pgm.GameMode = gm;

                                _SelectedModes.Add(pgm);
                            }
                        }
                    }
                }
                //Last value: Custom selection
                else if(_CurrentSelection == _SelectSlides[_SelectSlideGameMode].NumValues - 1)
                {
                    //TODO
                }
                //Only one game-mode will be used
                else
                {
                    if (_Whitelist.Count > 0 && _Whitelist.Count < _CurrentSelection - 1)
                    {
                        SPartyGameMode pgm = new SPartyGameMode();
                        pgm.GameMode = _Whitelist[_CurrentSelection - 1];

                        _SelectedModes.Add(pgm);                       
                    }
                    else if (_Whitelist.Count > 0 && _CurrentSelection - 1 - _Whitelist.Count < _OwnGameModes.Count)
                    {
                        _SelectedModes.Add(_OwnGameModes[_CurrentSelection - 1 - _Whitelist.Count]);
                    }
                    else
                    {
                        int numGameModes = Enum.GetValues(typeof(EGameMode)).Length;
                        if (_CurrentSelection - 1 < numGameModes)
                        {
                            SPartyGameMode pgm = new SPartyGameMode();
                            pgm.GameMode = (EGameMode)Enum.GetValues(typeof(EGameMode)).GetValue(_CurrentSelection - 1);

                            _SelectedModes.Add(pgm);
                        }
                        else if(_CurrentSelection - 1 - numGameModes < _OwnGameModes.Count)
                        {
                            _SelectedModes.Add(_OwnGameModes[_CurrentSelection - 1 - numGameModes]);
                        }
                    }
                        

                }
            }
            return true;
        }
    }
}