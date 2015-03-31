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
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Network;

namespace Vocaluxe.Screens
{
    public class CScreenHighscore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const int _NumEntrys = 10;
        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";
        private const string _TextHighScoreHelper = "HighScoreHelper";
        private const string _TextHighScoreText = "HighScoreText";


        private string[] _TextNumber;
        private string[] _TextName;
        private string[] _TextScore;
        private string[] _TextDate;
        private string[] _ParticleEffectNew;

        private List<SDBScoreEntry>[] _Scores;
        private SComResultScoreList[] _NetScores;
        private List<int> _NewEntryIDs;
        private int _Round;
        private int _Pos;
        private bool _IsDuet;
        private int _ScoreSource = 1;
        private int _NetScoreDifficulty = 2; //1=easy 2=normal 3=hard
        private bool _NeedUpdate = false;
        private bool _NetScoresAvailable = false;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        public override void Init()
        {
            base.Init();
            var texts = new List<string> { _TextSongName, _TextSongMode, _TextHighScoreHelper, _TextHighScoreText };

            _TextNumber = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextNumber[i] = "TextNumber" + (i + 1);
                texts.Add(_TextNumber[i]);
            }

            _TextName = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextName[i] = "TextName" + (i + 1);
                texts.Add(_TextName[i]);
            }

            _TextScore = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextScore[i] = "TextScore" + (i + 1);
                texts.Add(_TextScore[i]);
            }

            _TextDate = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextDate[i] = "TextDate" + (i + 1);
                texts.Add(_TextDate[i]);
            }

            _ParticleEffectNew = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
                _ParticleEffectNew[i] = "ParticleEffectNew" + (i + 1);

            _ThemeTexts = texts.ToArray();
            _ThemeParticleEffects = _ParticleEffectNew;

            _NewEntryIDs = new List<int>();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) { }
            else
            {
                if (_ScoreSource == 1)
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.Escape:
                        case Keys.Back:
                        case Keys.Enter:
                            _LeaveScreen();
                            break;
                        case Keys.Space:
                            if (_NetScoresAvailable)
                                _toggleScoreSource();
                            break;
                        case Keys.Down:
                            _ChangePos(1);
                            break;

                        case Keys.Up:
                            _ChangePos(-1);
                            break;

                        case Keys.Left:
                            _ChangeRound(-1);
                            break;

                        case Keys.Right:
                            _ChangeRound(1);
                            break;
                    }
                }
                else
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.Escape:
                        case Keys.Back:
                        case Keys.Enter:
                            _changeScoreSource(1);
                            break;
                        case Keys.Space:
                            _toggleScoreSource();
                            break;
                        case Keys.Down:
                            _ChangePos(1);
                            break;

                        case Keys.Up:
                            _ChangePos(-1);
                            break;
                        case Keys.Tab:
                            _toggleDifficulty();
                            break;

                        case Keys.Left:
                            _ChangeRound(-1);
                            break;

                        case Keys.Right:
                            _ChangeRound(1);
                            break;
                    }
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            { }
            if (_ScoreSource == 1)
            {


                if (mouseEvent.LB)
                    _LeaveScreen();

                if (mouseEvent.RB)
                    _LeaveScreen();

                if (mouseEvent.MB)
                {
                    int lastRound = _Round;
                    _ChangeRound(1);
                    if (lastRound == _Round)
                    {
                        _Round = 0;
                        _UpdateRound();
                    }
                }
            }
            else
            {
                if (mouseEvent.LB)
                    _changeScoreSource(1);

                if (mouseEvent.RB)
                    _changeScoreSource(1);

                if (mouseEvent.MB)
                {
                    _changeDifficulty(1);
                }
            }

            _ChangePos(mouseEvent.Wheel);
            return true;
        }

        public override bool UpdateGame()
        {
            if (!_NeedUpdate) { return true; }
            if (_ScoreSource == 1)
            {
                for (int p = 0; p < _NumEntrys; p++)
                {
                    if (_Pos + p < _Scores[_Round].Count)
                    {
                        _Texts[_TextNumber[p]].Visible = true;
                        _Texts[_TextName[p]].Visible = true;
                        _Texts[_TextScore[p]].Visible = true;
                        _Texts[_TextDate[p]].Visible = true;

                        _Texts[_TextNumber[p]].Text = (_Pos + p + 1).ToString();

                        string name = _Scores[_Round][_Pos + p].Name;
                        name += " [" + CLanguage.Translate(Enum.GetName(typeof(EGameDifficulty), _Scores[_Round][_Pos + p].Difficulty)) + "]";
                        if (_IsDuet)
                            name += " (P" + (_Scores[_Round][_Pos + p].VoiceNr + 1) + ")";
                        _Texts[_TextName[p]].Text = name;

                        _Texts[_TextScore[p]].Text = _Scores[_Round][_Pos + p].Score.ToString("00000");
                        _Texts[_TextDate[p]].Text = _Scores[_Round][_Pos + p].Date;

                        _ParticleEffects[_ParticleEffectNew[p]].Visible = _IsNewEntry(_Scores[_Round][_Pos + p].ID);
                    }
                    else
                    {
                        _Texts[_TextNumber[p]].Visible = false;
                        _Texts[_TextName[p]].Visible = false;
                        _Texts[_TextScore[p]].Visible = false;
                        _Texts[_TextDate[p]].Visible = false;
                        _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                    }
                }
            }
            else
            {
                List<SComResultScoreItem> scoreSource = null;

                if ((object)_NetScores[_Round] != null)
                {
                    if (_NetScoreDifficulty == 1 && _NetScores[_Round].easy != null)
                    {
                        if (_NetScores[_Round].easy.Count > 0)
                        {
                            scoreSource = _NetScores[_Round].easy;
                        }
                    }
                    if (_NetScoreDifficulty == 2 && _NetScores[_Round].medium != null)
                    {
                        if (_NetScores[_Round].medium.Count > 0)
                        {
                            scoreSource = _NetScores[_Round].medium;
                        }
                    }
                    if (_NetScoreDifficulty == 3 && _NetScores[_Round].hard != null)
                    {
                        if (_NetScores[_Round].hard.Count > 0)
                        {
                            scoreSource = _NetScores[_Round].hard;
                        }
                    }
                }

                for (int p = 0; p < _NumEntrys; p++)
                {
                    if (scoreSource != null && scoreSource.Count > _Pos + p)
                    {
                        SComResultScoreItem stat = scoreSource.ElementAt(_Pos + p);

                        _Texts[_TextNumber[p]].Visible = true;
                        _Texts[_TextName[p]].Visible = true;
                        _Texts[_TextScore[p]].Visible = true;
                        _Texts[_TextDate[p]].Visible = true;

                        _Texts[_TextNumber[p]].Text = (_Pos + p + 1).ToString();

                        string name = stat.Name;
                        if (_IsDuet && !String.IsNullOrEmpty(stat.VoiceNr))
                        {
                            name += " (P" + (stat.VoiceNr) + ")";
                        }

                        _Texts[_TextName[p]].Text = name;
                        _Texts[_TextScore[p]].Text = stat.Score;
                        _Texts[_TextDate[p]].Text = stat.Date;

                        _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                    }
                    else
                    {
                        _Texts[_TextNumber[p]].Visible = false;
                        _Texts[_TextName[p]].Visible = false;
                        _Texts[_TextScore[p]].Visible = false;
                        _Texts[_TextDate[p]].Visible = false;
                        _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                    }
                }
            }
            _NeedUpdate = false;
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            if (CCommunity.isEnabled())
            {
                if (CParty.CurrentPartyModeID > -1)
                {
                    var ComPartyOpts = CParty.GetCommunityOptions();
                    if (ComPartyOpts.CanLoadScores)
                    {
                        _NetScoresAvailable = true;
                    }
                }
                else
                {
                    _NetScoresAvailable = true;
                }
            }
            if (_NetScoresAvailable)
            {
                _Texts[_TextHighScoreHelper].Visible = true;
            }
            else
            {
                _Texts[_TextHighScoreHelper].Visible = false;
            }

            _Round = 0;
            _Pos = 0;
            _ScoreSource = 1;
            _NewEntryIDs.Clear();
            _AddScoresToDB();
            _LoadScores();
            _UpdateRound();

            UpdateGame();
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIDs.Any(t => t == id);
        }

        private void _AddScoresToDB()
        {
            CPoints points = CGame.GetPoints();
            if (points == null)
                return;

            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer[] players = points.GetPlayer(round, CGame.NumPlayers);

                for (int p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished && !CProfiles.IsGuestProfile(players[p].ProfileID))
                        _NewEntryIDs.Add(CDataBase.AddScore(players[p]));
                }
            }
        }

        private void _LoadScores()
        {
            _Pos = 0;
            int rounds = CGame.NumRounds;
            _Scores = new List<SDBScoreEntry>[rounds];
            _NetScores = new SComResultScoreList[rounds];

            for (int round = 0; round < rounds; round++)
            {
                int songID = CGame.GetSong(round).ID;
                EGameMode gameMode = CGame.GetGameMode(round);
                _Scores[round] = CDataBase.LoadScore(songID, gameMode);
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            CPoints points = CGame.GetPoints();
            CSong song = CGame.GetSong(_Round);
            if (song == null)
                return;

            _Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points.NumRounds > 1)
                _Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";

            switch (CGame.GetGameMode(_Round))
            {
                case EGameMode.TR_GAMEMODE_NORMAL:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;

                case EGameMode.TR_GAMEMODE_MEDLEY:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_MEDLEY";
                    break;

                case EGameMode.TR_GAMEMODE_DUET:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_DUET";
                    _IsDuet = true;
                    break;

                case EGameMode.TR_GAMEMODE_SHORTSONG:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_SHORTSONG";
                    break;

                default:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;
            }

            _Pos = 0;
            _NeedUpdate = true;
        }

        private void _ChangePos(int num)
        {
            if (_ScoreSource == 1)
            {
                if (_Scores[_Round].Count == 0)
                    _Pos = 0;
                else
                {
                    _Pos += num;
                    _Pos = _Pos.Clamp(0, _Scores[_Round].Count - 1, true);
                }
            }
            else
            {
                if (_NetScores[_Round].lastrefresh > 0)
                {
                    if (_NetScoreDifficulty == 1)
                    {
                        if (_NetScores[_Round].easy == null || _NetScores[_Round].easy.Count == 0)
                        {
                            _Pos = 0;
                        }
                        else
                        {
                            _Pos += num;
                            _Pos = _Pos.Clamp(0, _NetScores[_Round].easy.Count - 1, true);
                        }
                    }
                    else if (_NetScoreDifficulty == 2)
                    {
                        if (_NetScores[_Round].medium == null || _NetScores[_Round].medium.Count == 0)
                        {
                            _Pos = 0;
                        }
                        else
                        {
                            _Pos += num;
                            _Pos = _Pos.Clamp(0, _NetScores[_Round].medium.Count - 1, true);
                        }
                    }
                    else if (_NetScoreDifficulty == 3)
                    {
                        if (_NetScores[_Round].hard == null || _NetScores[_Round].hard.Count == 0)
                        {
                            _Pos = 0;
                        }
                        else
                        {
                            _Pos += num;
                            _Pos = _Pos.Clamp(0, _NetScores[_Round].hard.Count - 1, true);
                        }
                    }
                }
            }
            _NeedUpdate = true;
        }

        private void _ChangeRound(int num)
        {
            CPoints points = CGame.GetPoints();
            _Round += num;
            _Round = _Round.Clamp(0, points.NumRounds - 1);
            _UpdateRound();
            if (_ScoreSource == 2)
            {
                _GetComScores();
            }
        }

        private void _LeaveScreen()
        {
            CParty.LeavingHighscore();
        }

        private void _changeDifficulty(int num)
        {
            _NetScoreDifficulty += num;
            _NetScoreDifficulty = _NetScoreDifficulty.Clamp(1, 3, true);
            _setDifficultyName();
            _Pos = 0;
            _NeedUpdate = true;
        }

        private void _toggleDifficulty()
        {
            _NetScoreDifficulty += 1;
            if (_NetScoreDifficulty >= 4)
            {
                _NetScoreDifficulty = 1;
            }
            _setDifficultyName();
            _Pos = 0;
            _NeedUpdate = true;
        }


        private void _toggleScoreSource()
        {
            if (_ScoreSource == 1)
                _changeScoreSource(2);
            else
                _changeScoreSource(1);
        }

        private void _changeScoreSource(int source)
        {
            _Pos = 0;
            //Local
            if (source == 1)
            {
                _Texts[_TextHighScoreText].Text = "TR_SCREENHIGHSCORE_HIGHSCORE";
            }
            //community statistics
            else if (source == 2)
            {
                _setDifficultyName();
                _Round = 0;
                _GetComScores();
            }

            _ScoreSource = source;
            _NeedUpdate = true;
        }

        #region CommunityScores
        private void _setDifficultyName()
        {
            string difficultyName = "";
            switch (_NetScoreDifficulty)
            {
                case 1:
                    difficultyName = CLanguage.Translate("TR_CONFIG_EASY");
                    break;
                case 2:
                    difficultyName = CLanguage.Translate("TR_CONFIG_NORMAL");
                    break;
                case 3:
                    difficultyName = CLanguage.Translate("TR_CONFIG_HARD");
                    break;
            }
            _Texts[_TextHighScoreText].Text = CLanguage.Translate("TR_COMMUNITY_HS_TITLE").Replace("%v", difficultyName);
        }

        private void _GetComScores()
        {
            if (_NetScores[_Round].lastrefresh == 0)
            {
                _LoadNetScore(_Round);
            }
            _NeedUpdate = true;
        }

        private void _ComLoading()
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = CCommunity.getName();
            data.type = EPopupGeneralType.Loading;
            data.size = EPopupGeneralSize.Small;
            data.TextMessage = CLanguage.Translate("TR_COMMUNITY_HS_PROGRESS");
            popup.SetDisplayData(data);
            CGraphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        private void _ComAlert(string message)
        {
            var popup = CGraphics.GetPopup(EPopupScreens.PopupGeneral);
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyEscape,onKeyBack,onMouseLB", delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.name.Equals("onMouseLB") && eventData.target != null)
                {
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                else if (eventData.name.IndexOf("onKey") > -1)
                {
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                }
            });

            SPopupGeneral data = new SPopupGeneral();
            data.TextTitle = "Hiba";
            data.type = EPopupGeneralType.Alert;
            data.size = EPopupGeneralSize.Medium;
            data.ButtonOkLabel = "Ok";
            data.TextMessage = message;
            popup.SetDisplayData(data);
        }

        private void _LoadNetScore(int round)
        {
            CSong song = CGame.GetSong(round);
            EGameMode gameMode = CGame.GetGameMode(round);
            if (song.FileHash == "")
            {
                song.FileHash = CCommunity.hashTextFile(song.Folder, song.FileName);
            }

            _ComLoading();

            SComQueryHighScores data = new SComQueryHighScores();
            data.txthash = song.FileHash;

            if (CParty.CurrentPartyModeID > -1)
            {
                var ComPartyOpts = CParty.GetCommunityOptions();
                if (ComPartyOpts.CanLoadScores)
                {
                    data.username = ComPartyOpts.QueryUsername != null ? ComPartyOpts.QueryUsername : null;
                    data.password = ComPartyOpts.QueryPassword != null ? ComPartyOpts.QueryPassword : null;
                    data.queryType = ComPartyOpts.QueryType != null ? ComPartyOpts.QueryType : null;
                    data.method = ComPartyOpts.QueryMethod != null ? ComPartyOpts.QueryMethod : null;
                    data.gameMode = ComPartyOpts.QueryGameMode != null ? ComPartyOpts.QueryGameMode : gameMode.ToString();
                    if (ComPartyOpts.QueryDifficulty > 0)
                    {
                        data.difficulty = ComPartyOpts.QueryDifficulty;
                        _NetScoreDifficulty = ComPartyOpts.QueryDifficulty;
                    }
                    if (ComPartyOpts.QueryId > 0)
                    {
                        data.id = ComPartyOpts.QueryId;
                    }
                }
            }
            else
            {
                data.gameMode = gameMode.ToString();
            }

            CCommunity.getScoresAsync(data, delegate(SComResultScore scores)
            {
                if (scores.status == 1)
                {
                    CGraphics.HidePopup(EPopupScreens.PopupGeneral);
                    _NetScores[_Round] = scores.result;
                }
                else
                {
                    _NetScores[_Round] = new SComResultScoreList();
                    _ComAlert(scores.message);
                }
                _NeedUpdate = true;
            });
        }
        #endregion
    }
}