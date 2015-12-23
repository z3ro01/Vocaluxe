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

using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Songs;

namespace Vocaluxe.SongQueue
{
    interface ISongQueue
    {
        void Init();

        bool AddVisibleSong(int visibleIndex, ESongMode songMode, EGameMode gameMode = EGameMode.TR_GAMEMODE_NORMAL);
        bool AddSong(int absoluteIndex, ESongMode songMode, EGameMode gameMode = EGameMode.TR_GAMEMODE_NORMAL);
        bool RemoveVisibleSong(int visibleIndex);
        bool RemoveSong(int absoluteIndex);
        void ClearSongs();

        void Reset();
        void Start(SPlayer[] players);
        void StartNextRound(SPlayer[] players);
        bool IsFinished();

        CPoints GetPoints();

        int GetNumSongs();
        int GetCurrentRoundNr();

        CSong GetSong();
        CSong GetSong(int round);
        ESongMode GetCurrentSongMode();
        ESongMode GetSongMode(int round);
        EGameMode GetCurrentGameMode();
        EGameMode GetGameMode(int round);
    }
}