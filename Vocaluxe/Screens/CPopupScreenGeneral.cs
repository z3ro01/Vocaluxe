
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
using System.Collections.Generic;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Draw;

namespace Vocaluxe.Screens
{
    class CPopupScreenGeneral : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _StaticLoading = "StaticLoadingLoop";
        private const string _StaticSmallBg = "StaticPopupSmallBG";
        private const string _StaticProgressBar1 = "StaticProgressBar1";
        private const string _StaticProgressBar2 = "StaticProgressBar2";
        private const string _StaticProgressBar1Progress = "StaticProgressBar1Progress";
        private const string _StaticProgressBar2Progress = "StaticProgressBar2Progress";

        private const string _TextProgressBar1 = "TextMediumLoading1";
        private const string _TextProgressBar1Progress = "TextMediumPercent1";
        private const string _TextProgressBar2 = "TextMediumLoading2";
        private const string _TextProgressBar2Progress = "TextMediumPercent2";

        private const string _ButtonSmallYes = "ButtonSmallYes";
        private const string _ButtonSmallNo = "ButtonSmallNo";
        private const string _ButtonSmallOk = "ButtonSmallOk";
        private const string _TextSmallTitle = "TextSmallTitle";
        private const string _TextSmallMessage = "TextSmallMessage";

        private const string _StaticMediumBg = "StaticPopupMediumBG";
        private const string _ButtonMediumYes = "ButtonMediumYes";
        private const string _ButtonMediumNo = "ButtonMediumNo";
        private const string _ButtonMediumOk = "ButtonMediumOk";
        private const string _ButtonMediumUp = "ButtonMediumUp";
        private const string _ButtonMediumDown = "ButtonMediumDown";
        private const string _TextMediumTitle = "TextMediumTitle";
        private const string _TextMediumLoginUser = "TextMediumLoginUser";
        private const string _TextMediumLoginPassword = "TextMediumLoginPassword";
        private const string _ButtonUsername = "ButtonUsername";
        private const string _ButtonPassword = "ButtonPassword";
        private const int _MediumTextLines = 6;
        private const int _MediumMaxLineLength = 57;
        private string[] _TextMediums;

        private const string _StaticBigBg = "StaticPopupBigBG";
        private const string _ButtonBigYes = "ButtonBigYes";
        private const string _ButtonBigNo = "ButtonBigNo";
        private const string _ButtonBigOk = "ButtonBigOk";
        private const string _ButtonBigUp = "ButtonBigUp";
        private const string _ButtonBigDown = "ButtonBigDown";
        private const string _TextBigTitle = "TextBigTitle";
        private const int _BigTextLines = 13;
        private const int _BigMaxLineLength = 91;
        private string[] _TextBigs;

        private EPopupGeneralType _renderedDisplayMode = EPopupGeneralType.None;
        private SPopupGeneral _DisplayData = new SPopupGeneral();
        private bool _needUpdate = false;
        private List<string> _TextLines;
        private int _TextPos = 0;
        private int _animDirection = -1;
        private float _animAlpha = 1;
        private Timer _animTimer = null;
        private string themefile;
        private SPopupGeneralProgress _ProgressBar1 = new SPopupGeneralProgress();
        private SPopupGeneralProgress _ProgressBar2 = new SPopupGeneralProgress();
        private string editField = null;
 
        #region Event handling
        private List<evHandler> eventHandlers;

        public struct evHandler
        {
            public string type;
            public Action<SPopupGeneralEvent> callback;
        }

        public override void RemoveAllEventHandler()
        {
            eventHandlers = new List<evHandler>();
        }

        public override void AddEventHandler(string eventType, Action<SPopupGeneralEvent> callable)
        {
            if (eventHandlers == null) { eventHandlers = new List<evHandler>(); }
            evHandler current = new evHandler();
            current.type = eventType;
            current.callback = callable;
            eventHandlers.Add(current);
        }

        private void RunEventHandlers(string evName, string target = null)
        {
            SPopupGeneralEvent eventCall = new SPopupGeneralEvent();
            eventCall.Target = target;
            eventCall.Name = evName;
            eventHandlers.ForEach(delegate(evHandler eventData)
            {
                if (eventData.type.IndexOf(evName) > -1)
                {
                    eventData.callback(eventCall);
                }
            });
        }

        #endregion

        public override void LoadTheme(string xmlPath)
        {
            themefile = xmlPath;
            base.LoadTheme(xmlPath);
        }

        public override void SetDefaults()
        {
            _TextPos = 0;
            RemoveAllEventHandler();
        }


        public override void Init()
        {
            base.Init();
            var texts = new List<string> { _TextSmallTitle, _TextSmallMessage, _TextMediumTitle, _TextBigTitle, _TextProgressBar1, _TextProgressBar2, _TextProgressBar1Progress, _TextProgressBar2Progress, _TextMediumLoginUser, _TextMediumLoginPassword };
            _ThemeButtons = new string[] { _ButtonSmallNo, _ButtonSmallYes, _ButtonSmallOk, _ButtonMediumNo, _ButtonMediumYes, _ButtonMediumOk, _ButtonMediumUp, _ButtonMediumDown, _ButtonBigNo, _ButtonBigOk, _ButtonBigYes, _ButtonBigUp, _ButtonBigDown, _ButtonUsername, _ButtonPassword };

            _TextMediums = new string[_MediumTextLines];
            for (int i = 0; i < _MediumTextLines; i++)
            {
                _TextMediums[i] = "TextMediumMessage" + (i + 1);
                texts.Add(_TextMediums[i]);
            }

            _TextBigs = new string[_BigTextLines];
            for (int i = 0; i < _BigTextLines; i++)
            {
                _TextBigs[i] = "TextBigMessage" + (i + 1);
                texts.Add(_TextBigs[i]);
            }

            _ThemeTexts = texts.ToArray();
        }

        public override void OnClose()
        {
            if (_animTimer != null)
            {
                _animTimer.Enabled = false;
                _animTimer.Stop();
            }
            base.OnClose();
        }

        public override void OnShow()
        {
            base.OnShow();
            renderDisplayMode();
        }

        public override bool UpdateGame()
        {
            if (_DisplayData.Type == EPopupGeneralType.Loading)
                renderAnimation();

            if (_DisplayData.Type != _renderedDisplayMode)
            {
                renderDisplayMode();
                renderProgressBars();
            }
            else if (_needUpdate && _DisplayData.Type != EPopupGeneralType.None)
            {
                renderProgressBars();
                if (_DisplayData.Size == EPopupGeneralSize.Medium && _DisplayData.Type != EPopupGeneralType.Loading)
                {
                    if (_TextLines.Count > _MediumTextLines)
                    {
                        renderMediumText();

                    }
                }
                else if (_DisplayData.Size == EPopupGeneralSize.Big)
                {
                    if (_TextLines.Count > _BigTextLines)
                    {
                        renderBigText();
                    }
                }
                _needUpdate = false;
            }
            return true;
        }


        #region Keyboard / Mouse
        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (editField != null)
            {
                if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
                {
                    if (editField.Equals("ButtonUsername"))
                    {
                        _DisplayData.Username += keyEvent.Unicode;
                    }
                    else if (editField.Equals("ButtonPassword"))
                    {
                        _DisplayData.Password += keyEvent.Unicode;
                    }
                    renderEditables();
                    return true;
                }
                else if (keyEvent.Key == Keys.Back)
                {
                    if (editField.Equals("ButtonUsername"))
                    {
                        if (_DisplayData.Username != null && _DisplayData.Username.Length > 0) { _DisplayData.Username = _DisplayData.Username.Remove(_DisplayData.Username.Length - 1); }
                    }
                    else if (editField.Equals("ButtonPassword"))
                    {
                        if (_DisplayData.Password != null &&_DisplayData.Password.Length > 0) { _DisplayData.Password = _DisplayData.Password.Remove(_DisplayData.Password.Length - 1); }
                    }
                    renderEditables();
                    return true;
                
                }
                else if (keyEvent.Key == Keys.Return || keyEvent.Key == Keys.Tab)
                {
                    if (editField.Equals("ButtonUsername"))
                    {
                        editField = "ButtonPassword";
                        _Buttons[_ButtonPassword].Selected = true;
                        _Buttons[_ButtonUsername].Selected = false;
                        renderEditables();
                        return true;
                    }
                    else if (editField.Equals("ButtonPassword"))
                    {
                        editField = null;
                        _Buttons[_ButtonPassword].Selected = false;
                        _Buttons[_ButtonUsername].Selected = false;
                        _Buttons[_ButtonMediumYes].Selected = true;
                        renderEditables();
                        return true;
                    }
                    else
                    {
                        editField = null;
                        renderEditables();
                        return true;
                    }
                }
            }

            if (keyEvent.Key != Keys.None)
            {
                switch (keyEvent.Key)
                {
#if DEBUG
                    case Keys.Space:
                        ReloadTheme(themefile);
                        renderDisplayMode();
                        renderProgressBars();
                        break;
#endif
                    case Keys.Up:
                        scrollText(-1);
                        return true;
                    case Keys.Down:
                        scrollText(1);
                        return true;
                    case Keys.Left:
                        return base.HandleInput(keyEvent);
                    case Keys.Right:
                        return base.HandleInput(keyEvent);
                    case Keys.Escape:
                        if (editField != null)
                        {
                            editField = null;
                            renderEditables();
                            return true;
                        }
                        break;
                }

                foreach (string key in _ThemeButtons)
                {
                    if (_DisplayData.Type == EPopupGeneralType.Login)
                    {
                        if (_Buttons[key].Selected)
                        {
                            if (key.Equals("ButtonUsername") || key.Equals("ButtonPassword"))
                            {

                                if (keyEvent.Key == Keys.Return)
                                {
                                    if (editField == null)
                                    {
                                        editField = key;
                                    }
                                    else if(editField == key) {
                                         editField = null;
                                    }
                                    else { editField = key; }
                                    renderEditables();
                                }
                            }
                        }
                    }

                    if (_Buttons[key].Selected)
                    {
                        string buttonName = key.ToString().Replace("Medium", "").Replace("Small", "").Replace("Big", "");
                        if (!buttonName.Equals("ButtonUp") && !buttonName.Equals("ButtonDown"))
                        {
                            RunEventHandlers("onKey" + keyEvent.Key.ToString(), buttonName);
                            return true;
                        }
                        else
                        {
                            if (keyEvent.Key == Keys.Enter)
                            {
                                if (buttonName.Equals("ButtonDown"))
                                {
                                    scrollText(1);
                                    return true;
                                }
                                else if (buttonName.Equals("ButtonUp"))
                                {
                                    scrollText(-1);
                                    return true;
                                }
                            }
                        }
                    }
                }
                RunEventHandlers("onKey" + keyEvent.Key.ToString());
            }

            return base.HandleInput(keyEvent);
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (editField != null)
            {
                return true;
            }

            float z = CBase.Settings.GetZFar();
            int element = -1;
            for (int i = 0; i < _Elements.Count; i++)
            {
                if (!_IsSelectableOnPopup(i))
                    continue;
                if (!_IsMouseOverElementOnPopup(mouseEvent.X, mouseEvent.Y, _Elements[i]))
                    continue;
                element = i;
            }
            if (element > -1) { _SetSelected(element); }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                foreach (string key in _ThemeButtons)
                {
                    if (_DisplayData.Type == EPopupGeneralType.Login)
                    {
                        if (_Buttons[key].Selected)
                        {
                            if (key.Equals("ButtonUsername") || key.Equals("ButtonPassword"))
                            {
                                if (editField == null)
                                {
                                    editField = key;
                                }
                                else if (editField == key)
                                {
                                    editField = null;
                                }
                                else { editField = key; }
                                renderEditables();
                            }
                        }
                    }

                    if (_Buttons[key].Selected)
                    {
                        string buttonName = key.ToString().Replace("Medium", "").Replace("Small", "").Replace("Big", "");
                        if (!buttonName.Equals("ButtonUp") && !buttonName.Equals("ButtonDown"))
                        {
                            RunEventHandlers("onMouseLB", buttonName);
                            return true;
                        }
                        else
                        {
                            if (buttonName.Equals("ButtonDown"))
                            {
                                scrollText(1);
                                return true;
                            }
                            else if (buttonName.Equals("ButtonUp"))
                            {
                                scrollText(-1);
                                return true;
                            }
                        }
                    }
                }
                RunEventHandlers("onMouseLB");
            }

            if (mouseEvent.RB)
                RunEventHandlers("onMouseRB");

            return true;
        }
        #endregion

        #region Animation
        public void renderAnimation()
        {
            if (_Statics[_StaticLoading].Alpha != _animAlpha)
            {
                _Statics[_StaticLoading].Alpha = _animAlpha;
            }
        }


        public void startAnimation()
        {
            if (_animTimer == null)
            {
                _animTimer = new Timer();
                _animTimer.Tick += new EventHandler(_onTimerEvent);
            }
            if (_animTimer.Enabled != true)
            {
                _animTimer.Interval = 10;
                _animTimer.Enabled = true;
                _animTimer.Start();
            }
        }
       
        public void _onTimerEvent(object sender, EventArgs e)
        {
            if (_animDirection >= 0)
            {
                if (_animAlpha < 1)
                {
                    _animAlpha += 0.01f;
                    if (_animAlpha > 1) { _animAlpha = 1; _animDirection = 0; }
                }
                else
                {
                    _animDirection = -1;
                }
            }
            else
            {
                if (_animAlpha > 0)
                {
                    _animAlpha -= 0.01f;
                    if (_animAlpha < 0.1) { _animAlpha = 0; _animDirection = 1; }
                }
                else
                {
                    _animDirection = 1;
                }
            }
        }
        #endregion

        #region Public
        public override void SetProgressData(SPopupGeneralProgress data)
        {
            if (data.Total > 0 && data.Percentage == 0)
            {
                data.Percentage = (float)Math.Round(data.Loaded / data.Total * 100,2);
            }
            if (data.Target == 1) {
                _ProgressBar1 = data;
            }
            else { _ProgressBar2 = data; }
            _needUpdate = true;
        }

        public override SPopupGeneral GetDisplayData()
        {
            return _DisplayData;
        }

        public override void SetDisplayData(SPopupGeneral data)
        {
            editField = null;
            if (data.DefaultButton == null)
            {
                data.DefaultButton = "ButtonNo";
            }
            if (data.Type == EPopupGeneralType.Loading)
            {
                if (data.Size == EPopupGeneralSize.Big)
                {
                    data.Size = EPopupGeneralSize.Small;
                }

                if (EPopupGeneralSize.Small == data.Size)
                {
                    startAnimation();
                }
                else
                {
                    _ProgressBar1 = new SPopupGeneralProgress();
                    _ProgressBar2 = new SPopupGeneralProgress();
                    if (data.ProgressBar1Visible)
                    {
                        _ProgressBar1.Title = data.ProgressBar1Title;
                    }
                    if (data.ProgressBar2Visible)
                    {
                        _ProgressBar2.Title = data.ProgressBar2Title;
                    }
                }
            }
            _DisplayData = data;
        }
        #endregion

        #region Private
        private void renderLogin()
        {
            _Statics[_StaticMediumBg].Visible = true;
            if (_DisplayData.TextTitle != null)
            {
                _Texts[_TextMediumTitle].Text = _DisplayData.TextTitle;
                _Texts[_TextMediumTitle].Visible = true;
            }

            _Texts[_TextMediums[4]].Visible = true;

            if (_DisplayData.TextMessage != null)
            {
                _Texts[_TextMediums[4]].Text = _DisplayData.TextMessage;
            }

            _Texts[_TextMediumLoginUser].Visible     = true;
            _Texts[_TextMediumLoginPassword].Visible = true;

            _Buttons[_ButtonUsername].Visible = true;
            _Buttons[_ButtonPassword].Visible = true;

            _Buttons[_ButtonMediumYes].Text.Text = _DisplayData.ButtonYesLabel;
            _Buttons[_ButtonMediumYes].Visible = true;
            _Buttons[_ButtonMediumNo].Text.Text = _DisplayData.ButtonNoLabel;
            _Buttons[_ButtonMediumNo].Visible = true;

            if (_DisplayData.DefaultButton.Equals("ButtonYes"))
            {
                _SelectElement(_Buttons[_ButtonMediumYes]);
            }
            else if (_DisplayData.DefaultButton.Equals("ButtonNo"))
            {
                _SelectElement(_Buttons[_ButtonMediumNo]);
            }

            renderEditables();
        }

        private void renderEditables()
        {
            string dd = "";
            if (_DisplayData.Username != null) { _Buttons[_ButtonUsername].Text.Text = _DisplayData.Username; }
            else
            {
                _Buttons[_ButtonUsername].Text.Text = "";
            }
            if (_DisplayData.Password != null && _DisplayData.Password.Length > 0) { _Buttons[_ButtonPassword].Text.Text = dd.PadLeft(_DisplayData.Password.Length, '•'); }
            else
            {
                _Buttons[_ButtonPassword].Text.Text = "";
            }
            if (editField != null) { 
                if (editField.Equals("ButtonUsername"))
                {
                    _Buttons[_ButtonUsername].Text.Text += "|";
                }
                else if (editField.Equals("ButtonPassword"))
                {
                    _Buttons[_ButtonPassword].Text.Text += "|";
                }
            }
        }

        private void renderDisplayMode()
        {
            _Statics[_StaticSmallBg].Visible = false;
            _Statics[_StaticMediumBg].Visible = false;
            _Statics[_StaticBigBg].Visible = false;
            _Statics[_StaticLoading].Visible = false;
            _Statics[_StaticProgressBar1].Visible = false;
            _Statics[_StaticProgressBar2].Visible = false;
            _Statics[_StaticProgressBar1Progress].Visible = false;
            _Statics[_StaticProgressBar2Progress].Visible = false;

            foreach (string text in _ThemeTexts)
            {
                _Texts[text].Visible = false;
            }

            foreach (string btn in _ThemeButtons)
            {
                _Buttons[btn].Visible = false;
            }

            if (_DisplayData.Type == EPopupGeneralType.Login)
            {
                renderLogin();
            }
            else
            {

                if (_DisplayData.Size == EPopupGeneralSize.Small)
                {
                    _Statics[_StaticSmallBg].Visible = true;
                    if (_DisplayData.TextTitle != null)
                    {
                        _Texts[_TextSmallTitle].Text = _DisplayData.TextTitle;
                        _Texts[_TextSmallTitle].Visible = true;
                    }
                    if (_DisplayData.TextMessage != null)
                    {
                        _Texts[_TextSmallMessage].Text = _DisplayData.TextMessage;
                        _Texts[_TextSmallMessage].Visible = true;
                    }
                    if (_DisplayData.Type == EPopupGeneralType.Confirm)
                    {
                        _Buttons[_ButtonSmallYes].Text.Text = _DisplayData.ButtonYesLabel;
                        _Buttons[_ButtonSmallYes].Visible = true;
                        _Buttons[_ButtonSmallNo].Text.Text = _DisplayData.ButtonNoLabel;
                        _Buttons[_ButtonSmallNo].Visible = true;
                        if (_DisplayData.DefaultButton.Equals("ButtonYes"))
                        {
                            _SelectElement(_Buttons[_ButtonSmallYes]);
                        }
                        else if (_DisplayData.DefaultButton.Equals("ButtonNo"))
                        {
                            _SelectElement(_Buttons[_ButtonSmallNo]);
                        }
                    }
                    else if (_DisplayData.Type == EPopupGeneralType.Alert)
                    {
                        _Buttons[_ButtonSmallOk].Text.Text = _DisplayData.ButtonOkLabel;
                        _Buttons[_ButtonSmallOk].Visible = true;
                        _SelectElement(_Buttons[_ButtonSmallOk]);
                    }
                    else if (_DisplayData.Type == EPopupGeneralType.Loading)
                    {
                        _Statics[_StaticLoading].Visible = true;
                    }
                }
                else if (_DisplayData.Size == EPopupGeneralSize.Medium)
                {
                    if (_DisplayData.Type == EPopupGeneralType.Confirm || _DisplayData.Type == EPopupGeneralType.Alert)
                    {
                        _TextPos = 0;
                        _Statics[_StaticMediumBg].Visible = true;
                        if (_DisplayData.TextTitle != null)
                        {
                            _Texts[_TextMediumTitle].Text = _DisplayData.TextTitle;
                            _Texts[_TextMediumTitle].Visible = true;
                        }

                        _TextLines = calculateTextLines(_DisplayData.TextMessage, _MediumMaxLineLength);
                        if (_TextLines.Count > _MediumTextLines)
                        {
                            _Buttons[_ButtonMediumDown].Visible = true;
                        }

                        renderMediumText();
                    }

                    if (_DisplayData.Type == EPopupGeneralType.Confirm)
                    {
                        _Buttons[_ButtonMediumOk].Selected = false;
                        _Buttons[_ButtonMediumYes].Text.Text = _DisplayData.ButtonYesLabel;
                        _Buttons[_ButtonMediumYes].Visible = true;
                        _Buttons[_ButtonMediumNo].Text.Text = _DisplayData.ButtonNoLabel;
                        _Buttons[_ButtonMediumNo].Visible = true;
                        if (_DisplayData.DefaultButton.Equals("ButtonYes"))
                        {
                            _SelectElement(_Buttons[_ButtonMediumYes]);
                        }
                        else if (_DisplayData.DefaultButton.Equals("ButtonNo"))
                        {
                            _SelectElement(_Buttons[_ButtonMediumNo]);
                        }
                    }
                    else if (_DisplayData.Type == EPopupGeneralType.Alert)
                    {
                        _Buttons[_ButtonMediumNo].Selected = false;
                        _Buttons[_ButtonMediumYes].Selected = false;
                        _Buttons[_ButtonMediumYes].Visible = false;
                        _Buttons[_ButtonMediumNo].Visible = false;

                        _Buttons[_ButtonMediumOk].Text.Text = _DisplayData.ButtonOkLabel;
                        _Buttons[_ButtonMediumOk].Visible = true;
                        _Buttons[_ButtonMediumOk].Selected = true;
                    }
                    else if (_DisplayData.Type == EPopupGeneralType.Loading)
                    {
                        if (_DisplayData.TextTitle != null)
                        {
                            _Texts[_TextMediumTitle].Text = _DisplayData.TextTitle;
                            _Texts[_TextMediumTitle].Visible = true;
                        }
                        _Statics[_StaticMediumBg].Visible = true;
                        if (_DisplayData.ProgressBar1Visible)
                        {
                            _Statics[_StaticProgressBar1].Visible = true;
                            _Statics[_StaticProgressBar1Progress].Visible = true;
                            if (!_DisplayData.ProgressBar2Visible)
                            {
                                _Statics[_StaticProgressBar1].Y = 320;
                                _Statics[_StaticProgressBar1Progress].Y = 320;
                            }
                            _Statics[_StaticProgressBar1Progress].W = 0;
                        }
                        if (_DisplayData.ProgressBar2Visible)
                        {
                            _Statics[_StaticProgressBar2].Visible = true;
                            _Statics[_StaticProgressBar2Progress].Visible = true;
                            _Statics[_StaticProgressBar2Progress].W = 0;
                        }
                    }
                }
                else if (_DisplayData.Size == EPopupGeneralSize.Big)
                {
                    _TextPos = 0;
                    _Statics[_StaticBigBg].Visible = true;
                    if (_DisplayData.TextTitle != null)
                    {
                        _Texts[_TextBigTitle].Text = _DisplayData.TextTitle;
                        _Texts[_TextBigTitle].Visible = true;
                    }

                    _TextLines = calculateTextLines(_DisplayData.TextMessage, _BigMaxLineLength);
                    if (_TextLines.Count > _BigTextLines)
                    {
                        _Buttons[_ButtonBigDown].Visible = true;
                    }

                    renderBigText();

                    if (_DisplayData.Type == EPopupGeneralType.Confirm)
                    {
                        _Buttons[_ButtonBigYes].Text.Text = _DisplayData.ButtonYesLabel;
                        _Buttons[_ButtonBigYes].Visible = true;
                        _Buttons[_ButtonBigNo].Text.Text = _DisplayData.ButtonNoLabel;
                        _Buttons[_ButtonBigNo].Visible = true;
                        if (_DisplayData.DefaultButton.Equals("ButtonYes"))
                        {
                            _SelectElement(_Buttons[_ButtonBigYes]);
                        }
                        else if (_DisplayData.DefaultButton.Equals("ButtonNo"))
                        {
                            _SelectElement(_Buttons[_ButtonBigNo]);
                        }
                    }
                    else if (_DisplayData.Type == EPopupGeneralType.Alert)
                    {
                        _Buttons[_ButtonBigOk].Text.Text = _DisplayData.ButtonOkLabel;
                        _Buttons[_ButtonBigOk].Visible = true;
                        _Buttons[_ButtonBigOk].Selected = true;
                    }

                }
            }
            _renderedDisplayMode = _DisplayData.Type;
        }

        private void renderProgressBars() {
            int pbwidth = 640;
            if (_DisplayData.Type == EPopupGeneralType.Loading && _DisplayData.Size == EPopupGeneralSize.Medium)
            {
                if (_DisplayData.ProgressBar1Visible)
                {
                    if (_ProgressBar1.Title != null)
                    {
                        _Texts[_TextProgressBar1].Text = _ProgressBar1.Title;
                        _Texts[_TextProgressBar1].Visible = true;
                    }
                    if (_ProgressBar1.Percentage > -1)
                    {
                        _Texts[_TextProgressBar1Progress].Visible = true;
                        _Texts[_TextProgressBar1Progress].Text = _ProgressBar1.Percentage.ToString() + "%";
                        _Statics[_StaticProgressBar1Progress].W = (int)Math.Round(pbwidth * _ProgressBar1.Percentage / 100);
                    }
                    else
                    {
                        _Texts[_TextProgressBar1Progress].Visible = false;
                    }
                }
                if (_DisplayData.ProgressBar2Visible)
                {
                    if (_ProgressBar2.Title != null)
                    {
                        _Texts[_TextProgressBar2].Text = _ProgressBar2.Title;
                        _Texts[_TextProgressBar2].Visible = true;
                    }
                    if (_ProgressBar2.Percentage > -1)
                    {
                        _Texts[_TextProgressBar2Progress].Visible = true;
                        _Texts[_TextProgressBar2Progress].Text = _ProgressBar2.Percentage.ToString() + "%";
                        _Statics[_StaticProgressBar2Progress].W = (int)Math.Round(pbwidth * _ProgressBar2.Percentage / 100);
                    }
                    else
                    {
                        _Texts[_TextProgressBar2Progress].Visible = false;
                    }
                }
            }
        }

        private void renderMediumText()
        {
            for (int i = 0; i < _MediumTextLines; i++)
            {
                if (_TextPos + i < _TextLines.Count)
                {
                    _Texts[_TextMediums[i]].Text = _TextLines[i + _TextPos];
                    _Texts[_TextMediums[i]].Visible = true;
                }
                else
                {
                    _Texts[_TextMediums[i]].Visible = false;
                }
            }
        }

        private void renderBigText()
        {
            for (int i = 0; i < _BigTextLines; i++)
            {
                if (_TextPos + i < _TextLines.Count)
                {
                    _Texts[_TextBigs[i]].Text = _TextLines[i + _TextPos];
                    _Texts[_TextBigs[i]].Visible = true;
                }
                else
                {
                    _Texts[_TextBigs[i]].Visible = false;
                }
            }
        }

        private void scrollText(int num)
        {
            if (_DisplayData.Type == EPopupGeneralType.Alert || _DisplayData.Type == EPopupGeneralType.Confirm)
            {
                if (_DisplayData.Size == EPopupGeneralSize.Medium)
                {
                    if (_TextLines.Count > _MediumTextLines)
                    {
                        _TextPos += num;
                        _TextPos = _TextPos.Clamp(0, _TextLines.Count - _MediumTextLines, true);

                        if (_TextPos == 0)
                        {
                            _Buttons[_ButtonMediumUp].Visible   = false;
                            _Buttons[_ButtonMediumDown].Visible = true;
                        }
                        else if (_TextPos == _TextLines.Count - _MediumTextLines)
                        {
                            _Buttons[_ButtonMediumDown].Visible = false;
                            _Buttons[_ButtonMediumUp].Visible = true;
                        }
                        else
                        {
                            _Buttons[_ButtonMediumUp].Visible = true;
                            _Buttons[_ButtonMediumDown].Visible = true;
                        }

                        _needUpdate = true;
                    }
                }
                else if (_DisplayData.Size == EPopupGeneralSize.Big)
                {
                    if (_TextLines.Count > _BigTextLines)
                    {
                        _TextPos += num;
                        _TextPos = _TextPos.Clamp(0, _TextLines.Count - _BigTextLines, true);

                        if (_TextPos == 0)
                        {
                            _Buttons[_ButtonBigUp].Visible = false;
                            _Buttons[_ButtonBigDown].Visible = true;
                        }
                        else if (_TextPos == _TextLines.Count - _BigTextLines)
                        {
                            _Buttons[_ButtonBigUp].Visible = true;
                            _Buttons[_ButtonBigDown].Visible = false;
                        }
                        else
                        {
                            _Buttons[_ButtonBigUp].Visible = true;
                            _Buttons[_ButtonBigDown].Visible = true;
                        }
                        _needUpdate = true;
                    }
                }
            }
        }

        private List<string> calculateTextLines(string text, int maxlen)
        {
            List<string> linesOut = new List<string>();
            if (String.IsNullOrEmpty(text) || String.IsNullOrWhiteSpace(text)) { return linesOut; }
            string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > maxlen)
                {
                    string newline = "";
                    string[] words = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    for (int w = 0; w < words.Length; w++)
                    {
                        if (newline.Length + words[w].Length < maxlen)
                        {
                            if (newline.Length > 0) { newline += " " + words[w]; }
                            else { newline = words[w]; }

                            if (w + 1 < words.Length)
                            {
                                if (newline.Length + words[w + 1].Length > maxlen)
                                {
                                    linesOut.Add(newline);
                                    newline = "";
                                }
                            }
                        }
                        else
                        {
                            if (words[w].Length > maxlen)
                            {
                                int maxlenlong = maxlen - 10;
                                int count = (int)Math.Ceiling((float)(words[w].Length / maxlenlong));
                                for (int z = 0; z <= count; z++)
                                {
                                    int start = z * maxlenlong;
                                    int end = words[w].Length > (z * maxlenlong + maxlenlong) ? z * maxlenlong + maxlenlong : words[w].Length;
                                    linesOut.Add(words[w].Substring(start, end - start));
                                }
                            }
                            else
                            {
                                if (newline.Length > 0)
                                {
                                    linesOut.Add(newline);
                                    newline = words[w];
                                }
                            }
                        }
                    }

                    if (newline.Length > 0)
                    {
                        linesOut.Add(newline);
                    }
                }
                else
                {
                    linesOut.Add(lines[i]);
                }
            }
            return linesOut;
        }

        private bool _IsMouseOverElementOnPopup(int x, int y, CInteraction interact)
        {
            bool result = CHelper.IsInBounds(_GetElement(interact).Rect, x, y);
            if (result)
                return true;

            return false;
        }

        private bool _IsSelectableOnPopup(int element)
        {
            IMenuElement el = _GetElement(_Elements[element]);
            return el != null && (el.Selectable || CBase.Settings.GetProgramState() == EProgramState.EditTheme);
        }

        #endregion
    }
}