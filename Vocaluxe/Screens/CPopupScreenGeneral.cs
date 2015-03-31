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

        public struct evHandler
        {
            public string type;
            public Action<SPopupGeneralEvent> callback;
        }

        private List<evHandler> eventHandlers;

        public override void RemoveAllEventHandler()
        {
            eventHandlers = new List<evHandler>();
        }

        public override void OnClose()
        {
            base.OnClose();
            if (_animTimer != null)
            {
                _animTimer.Enabled = false;
                _animTimer.Stop();
            }
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

            Console.WriteLine("OutGoingEvent:" + evName + " / onTarget:" + target);
            SPopupGeneralEvent eventCall = new SPopupGeneralEvent();
            eventCall.target = target;
            eventCall.name = evName;
            eventHandlers.ForEach(delegate(evHandler eventData)
            {
                if (eventData.type.IndexOf(evName) > -1)
                {
                    eventData.callback(eventCall);
                }
            });
        }

        public override void SetDefaults()
        {
            _TextPos = 0;
            RemoveAllEventHandler();
        }

        public override void Init()
        {
            base.Init();
            var texts = new List<string> { _TextSmallTitle, _TextSmallMessage, _TextMediumTitle, _TextBigTitle };
            _ThemeButtons = new string[] { _ButtonSmallNo, _ButtonSmallYes, _ButtonSmallOk, _ButtonMediumNo, _ButtonMediumYes, _ButtonMediumOk, _ButtonMediumUp, _ButtonMediumDown, _ButtonBigNo, _ButtonBigOk, _ButtonBigYes, _ButtonBigUp, _ButtonBigDown };

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

        public override void OnShow()
        {
            base.OnShow();
            renderDisplayMode();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (!keyEvent.Key.ToString().Equals("None"))
            {
                switch (keyEvent.Key)
                {
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
                }

                foreach (string key in _ThemeButtons)
                {
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

        public override bool UpdateGame()
        {
            if (_DisplayData.type == EPopupGeneralType.Loading)
                renderAnimation();

            if (_DisplayData.type != _renderedDisplayMode)
            {
                renderDisplayMode();
            }
            else if (_needUpdate)
            {
                if (_DisplayData.size == EPopupGeneralSize.Medium)
                {
                    if (_TextLines.Count > _MediumTextLines)
                    {
                        renderMediumText();

                    }
                }
                else if (_DisplayData.size == EPopupGeneralSize.Big)
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

        public void renderDisplayMode()
        {
            _Statics[_StaticSmallBg].Visible = false;
            _Statics[_StaticMediumBg].Visible = false;
            _Statics[_StaticBigBg].Visible = false;
            _Statics[_StaticLoading].Visible = false;

            foreach (string text in _ThemeTexts)
            {
                _Texts[text].Visible = false;
            }
            foreach (string btn in _ThemeButtons)
            {
                _Buttons[btn].Visible = false;
            }

            if (_DisplayData.size == EPopupGeneralSize.Small)
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
                if (_DisplayData.type == EPopupGeneralType.Confirm)
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
                else if (_DisplayData.type == EPopupGeneralType.Alert)
                {
                    _Buttons[_ButtonSmallOk].Text.Text = _DisplayData.ButtonOkLabel;
                    _Buttons[_ButtonSmallOk].Visible = true;
                    _SelectElement(_Buttons[_ButtonSmallOk]);
                }
                else if (_DisplayData.type == EPopupGeneralType.Loading)
                {
                    _Statics[_StaticLoading].Visible = true;
                }
            }
            else if (_DisplayData.size == EPopupGeneralSize.Medium)
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
                    _Buttons[_ButtonMediumUp].Visible = true;
                    _Buttons[_ButtonMediumUp].Selectable = false;
                    _Buttons[_ButtonMediumDown].Visible = true;
                    _Buttons[_ButtonMediumDown].Selected = true;
                    _Buttons[_ButtonMediumDown].Selectable = false;
                }

                renderMediumText();

                if (_DisplayData.type == EPopupGeneralType.Confirm)
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
                else if (_DisplayData.type == EPopupGeneralType.Alert)
                {
                    _Buttons[_ButtonMediumNo].Selected = false;
                    _Buttons[_ButtonMediumYes].Selected = false;
                    _Buttons[_ButtonMediumOk].Text.Text = _DisplayData.ButtonOkLabel;
                    _Buttons[_ButtonMediumOk].Visible = true;
                    _Buttons[_ButtonMediumOk].Selected = true;
                }
            }
            else if (_DisplayData.size == EPopupGeneralSize.Big)
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
                    _Buttons[_ButtonBigUp].Visible = true;
                    _Buttons[_ButtonBigUp].Selectable = false;
                    _Buttons[_ButtonBigDown].Visible = true;
                    _Buttons[_ButtonBigDown].Selected = true;
                }

                renderBigText();

                if (_DisplayData.type == EPopupGeneralType.Confirm)
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
                else if (_DisplayData.type == EPopupGeneralType.Alert)
                {
                    _Buttons[_ButtonBigOk].Text.Text = _DisplayData.ButtonOkLabel;
                    _Buttons[_ButtonBigOk].Visible = true;
                    _Buttons[_ButtonBigOk].Selected = true;
                }

            }
            _renderedDisplayMode = _DisplayData.type;
        }

        public void renderMediumText()
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

        public void renderBigText()
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

        public void scrollText(int num)
        {
            if (_DisplayData.size == EPopupGeneralSize.Medium)
            {
                if (_TextLines.Count > _MediumTextLines)
                {
                    _TextPos += num;
                    _TextPos = _TextPos.Clamp(0, _TextLines.Count - _MediumTextLines, true);

                    if (_TextPos == 0)
                    {
                        _Buttons[_ButtonMediumUp].Selected = false;
                        // _Buttons[_ButtonMediumUp].Selectable = false;
                    }
                    else
                    {
                        // _Buttons[_ButtonMediumUp].Selectable = true;
                    }
                    if (_TextPos == _TextLines.Count - _MediumTextLines)
                    {
                        _Buttons[_ButtonMediumDown].Selected = false;
                        // _Buttons[_ButtonMediumDown].Selectable = false;
                    }
                    else
                    {
                        // _Buttons[_ButtonMediumDown].Selectable = true;
                    }
                    if (num > 0)
                    {
                        _Buttons[_ButtonMediumDown].Selected = true;
                    }
                    else
                    {
                        _Buttons[_ButtonMediumUp].Selected = true;
                    }
                    _needUpdate = true;
                }
            }
            else if (_DisplayData.size == EPopupGeneralSize.Big)
            {
                _TextPos += num;
                _TextPos = _TextPos.Clamp(0, _TextLines.Count - _BigTextLines, true);

                if (_TextPos == 0)
                {
                    _Buttons[_ButtonBigUp].Selected = false;
                    _Buttons[_ButtonBigUp].Selectable = false;
                }
                else
                {
                    _Buttons[_ButtonBigUp].Selectable = true;
                }
                if (_TextPos == _TextLines.Count - _BigTextLines)
                {
                    _Buttons[_ButtonBigDown].Selected = false;
                    _Buttons[_ButtonBigDown].Selectable = false;
                }
                else
                {
                    _Buttons[_ButtonBigDown].Selectable = true;
                }
                if (num > 0)
                {
                    _Buttons[_ButtonBigDown].Selected = true;
                }
                else
                {
                    _Buttons[_ButtonBigDown].Selected = true;
                }
                _needUpdate = true;
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

        public override void SetDisplayData(SPopupGeneral data)
        {
            _DisplayData = data;
            if (_DisplayData.DefaultButton == null)
            {
                _DisplayData.DefaultButton = "ButtonNo";
            }
            if (_DisplayData.type == EPopupGeneralType.Loading)
            {
                _DisplayData.size = EPopupGeneralSize.Small;
                startAnimation();
            }
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
    }
}