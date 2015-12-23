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
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.SongModes
{
    static class CSongModes
    {
        private static Dictionary<ESongMode, ISongMode> _SongModes;

        public static void Init()
        {
            _SongModes = new Dictionary<ESongMode, ISongMode>
                {
                    {ESongMode.TR_SONGMODE_NORMAL, new CGameModeNormal()},
                    {ESongMode.TR_SONGMODE_DUET, new CGameModeDuet()},
                    {ESongMode.TR_SONGMODE_SHORTSONG, new CGameModeShort()},
                    {ESongMode.TR_SONGMODE_MEDLEY, new CGameModeMedley()}
                };
        }

        public static ISongMode Get(ESongMode songMode)
        {
            if (_SongModes == null)
                return null;
            ISongMode result;
            if (!_SongModes.TryGetValue(songMode, out result))
                result = _SongModes[ESongMode.TR_SONGMODE_NORMAL];
            return result;
        }
    }

    class CGameModeNormal : CSongMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            return (song.IsSongModeAvailable(ESongMode.TR_SONGMODE_NORMAL)) ? song : null;
        }
    }

    class CGameModeDuet : CSongMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            return (song.IsSongModeAvailable(ESongMode.TR_SONGMODE_DUET)) ? song : null;
        }
    }

    class CGameModeShort : CSongMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            if (!song.IsSongModeAvailable(ESongMode.TR_SONGMODE_SHORTSONG))
                return null;
            var newSong = new CSong(song) {Finish = CGame.GetTimeFromBeats(song.ShortEnd.EndBeat, song.BPM) + CSettings.DefaultMedleyFadeOutTime + song.Gap};
            // set lines to short mode
            newSong.Notes.SetMedley(0, song.ShortEnd.EndBeat);

            return newSong;
        }
    }

    class CGameModeMedley : CSongMode
    {
        protected override CSong _PrepareSong(CSong song)
        {
            if (!song.IsSongModeAvailable(ESongMode.TR_SONGMODE_MEDLEY))
                return null;
            var newSong = new CSong(song) {Start = CGame.GetTimeFromBeats(song.Medley.StartBeat, song.BPM) - song.Medley.FadeInTime + song.Gap};
            if (newSong.Start < 0f)
                newSong.Start = 0f;

            newSong.Finish = CGame.GetTimeFromBeats(song.Medley.EndBeat, song.BPM) + song.Medley.FadeOutTime + song.Gap;

            // set lines to medley mode
            newSong.Notes.SetMedley(song.Medley.StartBeat, song.Medley.EndBeat);

            return newSong;
        }
    }
}