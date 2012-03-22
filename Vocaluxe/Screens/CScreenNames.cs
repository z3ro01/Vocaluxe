using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenNames : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private readonly string[] SelectSlidePlayerMic = new string[] { "SelectSlidePlayerMic1", "SelectSlidePlayerMic2", "SelectSlidePlayerMic3" };
        private readonly string[] ButtonPlayer = new string[] { "Button1Player", "Button2Player", "Button3Player" };
        private const string ButtonBack = "ButtonBack";
        private const string ButtonStart = "ButtonStart";
        private const string TextWarning = "TextWarning";
        private const string StaticWarning = "StaticWarning";
        
        public CScreenNames()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenNames";
            _ScreenVersion = ScreenVersion;

            List<string> statics = new List<string>();
            statics.Add(StaticWarning);
            _ThemeStatics = statics.ToArray();

            List<string> texts = new List<string>();
            foreach (string text in SelectSlidePlayerMic)
            {
                texts.Add(text);
            }
            _ThemeSelectSlides = texts.ToArray();

            texts.Clear();
            texts.Add(TextWarning);
            _ThemeTexts = texts.ToArray();

            texts.Clear();
            texts.Add(ButtonBack);
            texts.Add(ButtonStart);
            foreach (string text in ButtonPlayer)
            {
                texts.Add(text);
            }
            _ThemeButtons = texts.ToArray();
        }

        public override void LoadTheme()
        {
            base.LoadTheme();

            for (int i = 0; i < SelectSlidePlayerMic.Length; i++)
            {
                SelectSlides[htSelectSlides(SelectSlidePlayerMic[i])].WithTextures = true;
            }
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {

            }
            else
            {
                bool processed = false;
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        
                        for (int i = 0; i < ButtonPlayer.Length; i++)
                        {
                            if (Buttons[htButtons(ButtonPlayer[i])].Selected)
                            {
                                processed = true;
                                CGame.NumPlayer = i + 1;

                                UpdateSlides();
                                UpdateVisibility();

                                CConfig.NumPlayer = CGame.NumPlayer;
                                CConfig.SaveConfig();

                                CheckMics();

                                break;
                            }
                        }

                        if (!processed && Buttons[htButtons(ButtonBack)].Selected)
                        {
                            processed = true;
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (!processed && Buttons[htButtons(ButtonStart)].Selected)
                        {
                            processed = true;
                            StartSong();
                        }

                        break;
                }

                if (!processed)
                    UpdateSlides();
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {

            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                bool processed = false;
                for (int i = 0; i < ButtonPlayer.Length; i++)
                {
                    if (Buttons[htButtons(ButtonPlayer[i])].Selected)
                    {
                        processed = true;
                        CGame.NumPlayer = i + 1;

                        UpdateSlides();
                        UpdateVisibility();

                        CConfig.NumPlayer = CGame.NumPlayer;
                        CConfig.SaveConfig();

                        CheckMics();

                        break;
                    }
                }

                if (!processed && Buttons[htButtons(ButtonBack)].Selected)
                {
                    processed = true;
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (!processed && Buttons[htButtons(ButtonStart)].Selected)
                {
                    processed = true;
                    StartSong();
                }

                if (!processed)
                    UpdateSlides();
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenSong);
            }
            return true;
        }

        public override bool UpdateGame()
        {

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            UpdateSlides();
            UpdateVisibility();
            CheckMics();
        }

        public override bool Draw()
        {
            return base.Draw();
        }

        private void StartSong()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CProfiles.Profiles.Length > 0)
                {
                    int pIndex = SelectSlides[htSelectSlides(SelectSlidePlayerMic[i])].ValueIndex;
                    CGame.Player[i].Name = CProfiles.Profiles[pIndex].PlayerName;
                    CGame.Player[i].Difficulty = CProfiles.Profiles[pIndex].Difficulty;
                    CGame.Player[i].ProfileID = pIndex;
                }
                else
                {
                    CGame.Player[i].Name = "Player " + i.ToString();
                    CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                    CGame.Player[i].ProfileID = -1;
                }
            }

            CGraphics.FadeTo(EScreens.ScreenSing);
        }

        /// <summary>
        /// Update player selection slides when entering the screen and on every selection change.
        /// </summary>
        private void UpdateSlides()
        {
            Dictionary<int, int> indices = new Dictionary<int, int>();
            for (int i = 0; i < SelectSlidePlayerMic.Length; i++)
            {
                int index = SelectSlides[htSelectSlides(SelectSlidePlayerMic[i])].ValueIndex;
                if (i < CGame.NumPlayer && CProfiles.IsActive(index) && !indices.ContainsKey(index))
                    indices.Add(index, i);
                    
                SelectSlides[htSelectSlides(SelectSlidePlayerMic[i])].Clear();
            }

            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (!indices.ContainsValue(i))
                {
                    for (int j = 0; j < CProfiles.NumProfiles; j++)
                    {
                        if (CProfiles.IsActive(j) && !indices.ContainsKey(j))
                        {
                            indices.Add(j, i);
                            break;
                        }
                    }
                }
            }


            for (int i = 0; i < CProfiles.NumProfiles; i++)
            {
                if (CProfiles.IsActive(i))
                {
                    for (int p = 0; p < CGame.NumPlayer; p++)
                    {
                        if (!indices.ContainsKey(i) || indices[i] == p)
                        {
                            SelectSlides[htSelectSlides(SelectSlidePlayerMic[p])].AddValue(CProfiles.Profiles[i].PlayerName, CProfiles.Profiles[i].Avatar.Texture, i);

                            if (indices.ContainsKey(i) && indices[i] == p)
                                SelectSlides[htSelectSlides(SelectSlidePlayerMic[p])].SetSelectionByValueIndex(i);
                        }
                    }
                }
            }
        }

        private void UpdateVisibility()
        {
            for (int i = 0; i < SelectSlidePlayerMic.Length; i++)
            {
                SelectSlides[htSelectSlides(SelectSlidePlayerMic[i])].Visible = i < CGame.NumPlayer;
            }
        }

        private void CheckMics()
        {
            List<int> _PlayerWithoutMicro = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (!CConfig.IsMicConfig(player + 1))
                {
                    _PlayerWithoutMicro.Add(player + 1);
                }
            }
            if (_PlayerWithoutMicro.Count > 0)
            {
                Statics[htStatics(StaticWarning)].Visible = true;
                Texts[htTexts(TextWarning)].Visible = true;

                if (_PlayerWithoutMicro.Count > 1)
                {
                    string PlayerNums = string.Empty;
                    for (int i = 0; i < _PlayerWithoutMicro.Count; i++)
                    {
                        if (_PlayerWithoutMicro.Count - 1 == i)
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString();
                        }
                        else if (_PlayerWithoutMicro.Count - 2 == i)
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        }
                        else
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + ", ";
                        }
                    }

                    Texts[htTexts(TextWarning)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PL").Replace("%v", PlayerNums);
                }
                else
                {
                    Texts[htTexts(TextWarning)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_SG").Replace("%v", _PlayerWithoutMicro[0].ToString());
                }
            }
            else
            {
                Statics[htStatics(StaticWarning)].Visible = false;
                Texts[htTexts(TextWarning)].Visible = false;
            }
        }
    }
}
