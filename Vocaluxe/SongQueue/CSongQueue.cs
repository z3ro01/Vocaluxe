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
using Vocaluxe.Base;
using Vocaluxe.SongModes;
using Vocaluxe.GameModes;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Songs;

namespace Vocaluxe.SongQueue
{
    struct SSongQueueEntry
    {
        public readonly int SongID;
        public readonly ESongMode SongMode;
        public readonly EGameMode GameMode;

        public SSongQueueEntry(int songID, ESongMode songMode, EGameMode gameMode)
        {
            SongID = songID;
            SongMode = songMode;
            GameMode = gameMode;
        }
    }

    class CSongQueue : ISongQueue
    {
        private List<SSongQueueEntry> _SongQueue;
        private int _CurrentRound;
        private CPoints _Points;
        private CSong _CurrentSong;
        private CGameMode _CurrentGameMode;

        #region Implementation
        public void Init()
        {
            _SongQueue = new List<SSongQueueEntry>();
            Reset();
            CSongModes.Init();
        }

        public bool AddVisibleSong(int visibleIndex, ESongMode songMode, EGameMode gameMode = EGameMode.TR_GAMEMODE_NORMAL)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _AddSong(CSongs.VisibleSongs[visibleIndex].ID, songMode, gameMode);
        }

        public bool AddSong(int absoluteIndex, ESongMode songMode, EGameMode gameMode = EGameMode.TR_GAMEMODE_NORMAL)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _AddSong(CSongs.AllSongs[absoluteIndex].ID, songMode, gameMode);
        }

        private bool _AddSong(int songID, ESongMode songMode, EGameMode gameMode = EGameMode.TR_GAMEMODE_NORMAL)
        {
            if (!CSongs.GetSong(songID).IsSongModeAvailable(songMode))
                return false;

            _SongQueue.Add(new SSongQueueEntry(songID, songMode, gameMode));
            return true;
        }

        public bool RemoveVisibleSong(int visibleIndex)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _RemoveSong(CSongs.VisibleSongs[visibleIndex].ID);
        }

        public bool RemoveSong(int absoluteIndex)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _RemoveSong(CSongs.AllSongs[absoluteIndex].ID);
        }

        private bool _RemoveSong(int songID)
        {
            for (int i = 0; i < _SongQueue.Count; i++)
            {
                if (_SongQueue[i].SongID != songID)
                    continue;
                _SongQueue.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void ClearSongs()
        {
            _SongQueue.Clear();
        }

        public void Reset()
        {
            _CurrentRound = -1;
        }

        public void Start(SPlayer[] players)
        {
            _Points = new CPoints(_SongQueue.Count, players);
        }

        public void StartNextRound(SPlayer[] players)
        {
            if (IsFinished())
                return;
            if (_CurrentRound > -1)
            {
                _Points.SetPoints(
                    _CurrentRound,
                    _SongQueue[_CurrentRound].SongID,
                    players,
                    _SongQueue[_CurrentRound].SongMode);
            }
            _CurrentRound++;
            _CurrentSong = IsFinished() ? null : CSongModes.Get(GetCurrentSongMode()).GetSong(_SongQueue[_CurrentRound].SongID);
            if (IsFinished())
                _CurrentGameMode = new CGameModeNormal();
            else
            {
                switch (_SongQueue[_CurrentRound].GameMode)
                {
                    case EGameMode.TR_GAMEMODE_NORMAL:
                        _CurrentGameMode = new CGameModeNormal();
                        break;

                    case EGameMode.TR_GAMEMODE_BLIND:
                        _CurrentGameMode = new CGameModeBlind();
                        break;

                    case EGameMode.TR_GAMEMODE_UNTIL5000:
                        _CurrentGameMode = new CGameModeUntil5000();
                        break;

                    case EGameMode.TR_GAMEMODE_HOLDTHELINE:
                        _CurrentGameMode = new CGameModeHoldTheLine();
                        break;

                    default:
                        _CurrentGameMode = new CGameModeNormal();
                        break;
                }
            }
        }

        public bool IsFinished()
        {
            return _CurrentRound >= _SongQueue.Count || _SongQueue.Count == 0;
        }

        /// <summary>
        ///     Get current round nr (1 ~ n)
        /// </summary>
        /// <returns>current round nr (1 ~ n)</returns>
        public int GetCurrentRoundNr()
        {
            return _CurrentRound + 1;
        }

        /// <summary>
        ///     Returns current round
        /// </summary>
        /// <returns>Current round (0 based)</returns>
        public int GetCurrentRound()
        {
            return _CurrentRound;
        }

        /// <summary>
        ///     Get current song
        /// </summary>
        /// <returns>Song of current round or null if there is none/game finished</returns>
        public CSong GetSong()
        {
            return _CurrentSong;
        }

        public int GetNumSongs()
        {
            return _SongQueue.Count;
        }

        /// <summary>
        ///     Get song of specified round
        /// </summary>
        /// <param name="round">Round (0 based)</param>
        /// <returns>Current song or null if out of bounds</returns>
        public CSong GetSong(int round)
        {
            if (round == _CurrentRound)
                return _CurrentSong;
            if (round < _SongQueue.Count && round >= 0)
                return CSongs.GetSong(_SongQueue[round].SongID);

            return null;
        }

        /// <summary>
        ///     Get songMode of specified round
        /// </summary>
        /// <param name="round">Round (0 based)</param>
        /// <returns>Current song-mode or TR_SONGMODE_NORMAL if out of bounds</returns>
        public ESongMode GetSongMode(int round)
        {
            if (round < _SongQueue.Count && round >= 0)
                return _SongQueue[round].SongMode;

            return ESongMode.TR_SONGMODE_NORMAL;
        }

        public ESongMode GetCurrentSongMode()
        {
            return GetSongMode(_CurrentRound);
        }

        public EGameMode GetGameModeName(int round)
        {
            if (round < _SongQueue.Count && round >= 0)
                return _SongQueue[round].GameMode;

            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public EGameMode GetCurrentGameModeName()
        {
            return GetGameModeName(_CurrentRound);
        }


        public CGameMode GetCurrentGameMode()
        {
            return _CurrentGameMode;
        }

        public CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation
    }
}