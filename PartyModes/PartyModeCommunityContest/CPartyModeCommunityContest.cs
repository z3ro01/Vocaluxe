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
using System.Linq;
using System.Runtime.InteropServices;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;
using VocaluxeLib.Network;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.CommunityContest
{
    public enum ESongSource
    {
        // ReSharper disable InconsistentNaming
        TR_ALLSONGS,
        TR_CATEGORY,
        TR_PLAYLIST
        // ReSharper restore InconsistentNaming
    }

    public class CRound
    {
        public int SongID;
        public int SingerTeam1;
        public int SingerTeam2;
        public int PointsTeam1;
        public int PointsTeam2;
        public int Winner;
        public bool Finished;
    }

    public abstract class CPartyScreenCommunityContest : CMenuParty
    {
        protected new CPartyModeCommunityContest _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeCommunityContest)base._PartyMode;
        }
    }

    // ReSharper disable ClassNeverInstantiated.Global
    public sealed class CPartyModeCommunityContest : CPartyMode
    // ReSharper restore ClassNeverInstantiated.Global
    {
        public override int MinMics
        {
            get { return 1; }
        }
        public override int MaxMics
        {
            get { return 1; }
        }
        public override int MinPlayers
        {
            get { return 1; }
        }
        public override int MaxPlayers
        {
            get { return 6; }
        }
        public override int MinTeams
        {
            get { return 1; }
        }
        public override int MaxTeams
        {
            get { return 1; }
        }

        public override int MinPlayersPerTeam
        {
            get { return MinPlayers; }
        }
        public override int MaxPlayersPerTeam
        {
            get { return MaxPlayers; }
        }

        private enum EStage
        {
            Config,
            Names,
            Main,
            Singing
        }

        public struct SData
        {
            public int SelectedContestId;
            public SComResultContestItem SelectedContest;

            public int NumPlayerTeam1;
            public int NumPlayerTeam2;
            public int NumFields;
            public int Team;

            public List<int> ProfileIDs;
            public ESongSource SongSource;
            public int CategoryIndex;
            public int PlaylistID;

            public EGameMode GameMode;

            public List<CRound> Rounds;
            public List<int> Songs;

            public int CurrentRoundNr;
            public int FieldNr;

            public int[] NumJokerRandom;
            public int[] NumJokerRetry;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModeCommunityContest(int id)
            : base(id)
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] { 5, 5 };
            _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.NoDuets;

            GameData = new SData
            {
                NumPlayerTeam1 = 2,
                NumPlayerTeam2 = 2,
                NumFields = 9,
                ProfileIDs = new List<int>(),
                CurrentRoundNr = 0,
                FieldNr = 0,
                SongSource = ESongSource.TR_ALLSONGS,
                PlaylistID = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                Rounds = new List<CRound>(),
                Songs = new List<int>(),
                NumJokerRandom = new int[2],
                NumJokerRetry = new int[2]
            };
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _Stage = EStage.Config;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            GameData.Songs.Clear();
            GameData.Rounds.Clear();
           
            return true;
        }

        public override void UpdateGame()
        {
            /*
            if (CBase.Songs.IsInCategory() || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _ScreenSongOptions.Selection.RandomOnly = true;
            else
                _ScreenSongOptions.Selection.RandomOnly = false;*/
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenCommunityContestConfig"];
                case EStage.Names:
                    return _Screens["CPartyScreenCommunityContestNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenCommunityContestMain"];
                case EStage.Singing:
                    return CBase.Graphics.GetScreen(EScreen.Sing);
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
        }

        private void _FadeToScreen()
        {
            if (CBase.Graphics.GetNextScreen() != _GetNextScreen())
                CBase.Graphics.FadeTo(_GetNextScreen());
        }

        public void Next()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    _Stage = EStage.Names;
                    break;
                case EStage.Names:
                    Console.Write("here we are");
                    _Stage = EStage.Main;
                    GameData.Team = 0;
                    CBase.Songs.ResetSongSung();
                    GameData.CurrentRoundNr = 1;
                    _CreateRounds();
                    _SetNumJokers();
                    _PreparePlayerList(0);
                   
                    break;
                case EStage.Main:
                    _Stage = EStage.Singing;
                    if (!_StartRound())
                    {
                        Console.Write("Cant start round...");
                        return;
                    }

                    break;
                case EStage.Singing:
                    _Stage = EStage.Main;
                    GameData.Team = GameData.Team == 1 ? 0 : 1;
                    _UpdatePlayerList();
                    break;
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }

            _FadeToScreen();
        }

        public void Back()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    CBase.Graphics.FadeTo(EScreen.Party);
                    return;
                case EStage.Names:
                    _Stage = EStage.Config;
                    break;
                case EStage.Main:
                    _Stage = EStage.Names;
                    break;
                default: // Rest is not allowed
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
            _FadeToScreen();
        }

        public override IMenu GetStartScreen()
        {
            return _Screens["CPartyScreenCommunityContestConfig"];
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            throw new ArgumentException("Not required!");
        }

        public override void JokerUsed(int teamNr) { }

        public override void SongSelected(int songID)
        {
            throw new ArgumentException("Not required!");
        }

        public override void LeavingHighscore()
        {
            CBase.Songs.AddPartySongSung(CBase.Game.GetSong(0).ID);
            _UpdateScores();
            Next();
        }

        private void _CreateRounds()
        {
            GameData.Rounds = new List<CRound>();
            for (int i = 0; i < GameData.NumFields; i++)
            {
                var r = new CRound();
                GameData.Rounds.Add(r);
            }
        }

        private void _PreparePlayerList(int team)
        {
           
        }

        public void UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
                return;

            switch (GameData.SongSource)
            {
                case ESongSource.TR_PLAYLIST:
                    for (int i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistID); i++)
                    {
                        int id = CBase.Playlist.GetSong(GameData.PlaylistID, i).SongID;
                        if (CBase.Songs.GetSongByID(id).AvailableGameModes.Contains(GameData.GameMode))
                            GameData.Songs.Add(id);
                    }
                    break;

                case ESongSource.TR_ALLSONGS:
                    ReadOnlyCollection<CSong> avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(GameData.GameMode)).Select(song => song.ID));
                    break;

                case ESongSource.TR_CATEGORY:
                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(GameData.GameMode)).Select(song => song.ID));

                    CBase.Songs.SetCategory(-1);
                    break;
            }
            GameData.Songs.Shuffle();
        }

        private void _UpdatePlayerList()
        {
        }

        private bool _StartRound()
        {
            Console.WriteLine("YEEEEP");
            CBase.Game.Reset();
            CBase.Game.ClearSongs();


            CBase.Game.SetNumPlayer(1);

            SPlayer[] players = CBase.Game.GetPlayers();

            Console.WriteLine("Players " + players.Length);
            if (players == null)
                return false;

            if (players.Length < 1)
                return false;

            CRound round = GameData.Rounds[0];
            bool isDuet = CBase.Songs.GetSongByID(round.SongID).IsDuet;

            for (int i = 0; i < 2; i++)
            {
                //default values
                players[i].ProfileID = -1;
            }

            //try to fill with the right data
            players[0].ProfileID = GameData.ProfileIDs[0];
            if (isDuet)
                players[0].VoiceNr = 0;

            CBase.Game.AddSong(round.SongID, GameData.GameMode);
            return true;
        }

        private void _SetNumJokers()
        {
            switch (GameData.NumFields)
            {
                case 9:
                    GameData.NumJokerRandom[0] = 1;
                    GameData.NumJokerRandom[1] = 1;
                    GameData.NumJokerRetry[0] = 0;
                    GameData.NumJokerRetry[1] = 0;
                    break;

                case 16:
                    GameData.NumJokerRandom[0] = 2;
                    GameData.NumJokerRandom[1] = 2;
                    GameData.NumJokerRetry[0] = 1;
                    GameData.NumJokerRetry[1] = 1;
                    break;

                case 25:
                    GameData.NumJokerRandom[0] = 3;
                    GameData.NumJokerRandom[1] = 3;
                    GameData.NumJokerRetry[0] = 2;
                    GameData.NumJokerRetry[1] = 2;
                    break;
            }
        }

        private void _UpdateScores()
        {
            CPoints _Points = CBase.Game.GetPoints();
            if (_Points == null || _Points.NumPlayer < 1)
                return;

            var toSend = new SComQueryScore();
            bool songfinished = false;

            SPlayer[] players = _Points.GetPlayer(0, 1);
            toSend.scores = new SComQueryScoreItem[_Points.NumRounds * players.Length];
            int scindex = 0;

            for (int round = 0; round < _Points.NumRounds; round++)
            {
                players = _Points.GetPlayer(round, 1);
                for (int p = 0; p < players.Length; p++)
                {
                    //if player has communityProfile
                    var nprofile = CBase.Profiles.GetCommunityProfile(players[p].ProfileID);
                    if (String.IsNullOrEmpty(nprofile))
                    {
                        continue;
                    }
                    else
                    {
                        SComProfile nprofiledata = CCommunity.getProfileByFile(nprofile);
                        if (String.IsNullOrEmpty(nprofiledata.Email))
                        {
                            continue;
                        }
                        else
                        {
                            var pdata = new SComQueryScoreItem();
                            pdata.score = players[p].Points;
                            pdata.playerId = players[p].ProfileID;
                            pdata.playerName = CBase.Profiles.GetPlayerName(players[p].ProfileID, p);
                            pdata.username = nprofiledata.Email;
                            pdata.password = nprofiledata.Password;
                            pdata.gameMode = players[p].GameMode.ToString();
                            pdata.goldenbonus = players[p].PointsGoldenNotes;
                            pdata.linebonus = players[p].PointsLineBonus;
                            pdata.difficulty = CBase.Profiles.GetDifficulty(players[p].ProfileID).ToString();
                            if (players[p].SongFinished)
                            {
                                songfinished = true;
                            }
                            if (players[p].GameMode == EGameMode.TR_GAMEMODE_DUET)
                            {
                                pdata.voicenr = players[p].VoiceNr + 1;
                            }

                            if (string.IsNullOrEmpty(toSend.gameMode))
                            {
                                toSend.gameMode = pdata.gameMode;
                            }

                            CSong song = CBase.Songs.GetSongByID(players[p].SongID);
                            pdata.artist = song.Artist;
                            pdata.title = song.Title;
                            pdata.txtHash = string.IsNullOrEmpty(song.FileHash) ? CCommunity.hashTextFile(song.Folder, song.FileName) : song.FileHash;
                            pdata.round = round;

                            if (string.IsNullOrEmpty(song.FileHash))
                            {
                                song.FileHash = pdata.txtHash;
                            }
                            toSend.scores[scindex] = pdata;
                            scindex++;
                        }
                    }
                }
            }

            if (scindex > 0) {
               // if (songfinished) { 
                    toSend.guests = 0;
                    toSend.method = "setcontestscore";
                    CCommunity.setScores(toSend);
                    //send scores
                    sendScoresToServer();
             /*   }
                else
                {
                    Console.WriteLine("Not finished... try again?");
                }*/
            }
        }

        private void sendScoresToServer() {
            showLoadingPopup("Sending scores please wait...");
            if (CCommunity.canSendNow()){
                CCommunity.sendScoreAsync(delegate(SComResponse result)
                {
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    if (result.status == 0)
                    {
                        //dont let go to next screen if score doesnt saved
                        showConfirmPopup("Cant send score", "You could try again or exit:\n "+result.message, delegate(SPopupGeneralEvent eventData) {
                            Console.WriteLine("You pressed: " + eventData.name+ " / "+eventData.target);

                            if (eventData.name.Equals("onKeyReturn") || eventData.name.Equals("onMouseLB"))
                            {
                                if (eventData.target.Equals("ButtonYes"))
                                {
                                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                                    sendScoresToServer();
                                }
                                else if (eventData.target.Equals("ButtonCancel"))
                                {
                                    CBase.Graphics.FadeTo(EScreen.Party);
                                }
                            }
                        },"Try agaqin", "Exit PartyMode");
                        
                    }
                });
            }
        }

        public void showConfirmPopup(string title, string message, Action<SPopupGeneralEvent> callback, string buttonYesLabel = null, string buttonNoLabel = null)
        {
            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            SPopupGeneral data = new SPopupGeneral();
            popup.SetDefaults();
            popup.AddEventHandler("onKeyReturn,onKeyBack,onKeyEscape,onMouseLB", callback);

            data.type = EPopupGeneralType.Confirm;
            data.size = EPopupGeneralSize.Big;
            data.TextTitle = _Translate(title);
            data.TextMessage = message;
            data.ButtonYesLabel = String.IsNullOrEmpty(buttonYesLabel) ? _Translate("TR_BUTTON_PLAY") : _Translate(buttonYesLabel);
            data.ButtonNoLabel = String.IsNullOrEmpty(buttonNoLabel) ? "TR_BUTTON_BACK" : _Translate(buttonNoLabel);

            popup.SetDisplayData(data);
            CBase.Graphics.ShowPopup(EPopupScreens.PopupGeneral);
        }

        public void showLoadingPopup(string title)
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

        public string _Translate(string str, KeyValuePair<string, string>[] kvpair = null)
        {
            string translated = CBase.Language.Translate(str, ID);
            if (kvpair != null)
            {
                if (kvpair.Length > 0)
                {
                    for (int i = 0; i < kvpair.Length; i++)
                    {
                        translated = translated.Replace("%" + kvpair[i].Key, kvpair[i].Value);
                    }
                }
            }
            return translated;
        }
    }
}