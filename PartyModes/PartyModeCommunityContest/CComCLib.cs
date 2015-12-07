using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using VocaluxeLib.Network;

namespace VocaluxeLib.PartyModes.CommunityContest
{
    public struct SNetstatPlaylists
    {
        public int playlistId;
        public bool readyToUse;
        public List<SComResultCPlaylistItem> items;
    }

    public struct SNetstatCompareInfo
    {
        public bool status;
        public int playlistId;
        public int ok;
        public int count;
        public List<SComResultCPlaylistItem> wrongHashes;
        public List<SComResultCPlaylistItem> missing;
    }

    public static class CComCLib
    {
        public static List<SNetstatPlaylists> PlayLists = new List<SNetstatPlaylists>();
        public static SNetstatCompareInfo CompareInfo = new SNetstatCompareInfo();

        public static void addRemotePlaylist(List<SComResultCPlaylistItem> playlist, int remoteId)
        {
            SNetstatPlaylists newplaylist = new SNetstatPlaylists();
            newplaylist.playlistId = remoteId;
            newplaylist.readyToUse = false;
            newplaylist.items = playlist;

            var index = PlayLists.FindIndex(item => item.playlistId == remoteId);
            if (index > -1)
            {
                PlayLists[index] = newplaylist;
            }
            else
            {
                PlayLists.Add(newplaylist);
            }
        }

        public static void _SetPlaylistSongId(int index, int itemindex, int songID)
        {
            if (PlayLists[index].items.Count > 0)
            {
                SComResultCPlaylistItem item = new SComResultCPlaylistItem();
                item.artist = PlayLists[index].items[itemindex].artist;
                item.title = PlayLists[index].items[itemindex].title;
                item.localId = songID;
                item.hash = PlayLists[index].items[itemindex].hash;
                item.files = PlayLists[index].items[itemindex].files;
                item.txtUrl = PlayLists[index].items[itemindex].txtUrl;
                PlayLists[index].items[itemindex] = item;
            }
        }

        public static void _SetPlaylistReadyStatus(int index, bool ready)
        {
            if (PlayLists[index].items.Count > 0)
            {
               SNetstatPlaylists newplaylist = new SNetstatPlaylists();
               newplaylist.playlistId = PlayLists[index].playlistId;
               newplaylist.readyToUse = ready;
               newplaylist.items = PlayLists[index].items;
               PlayLists[index] = newplaylist;
           }
        }

        public static bool ComparePlaylist(int remoteId)
        {
            CompareInfo = new SNetstatCompareInfo();
            CompareInfo.status = false;


            var index = PlayLists.FindIndex(item => item.playlistId == remoteId);
            if (index > -1)
            {
                if (PlayLists[index].readyToUse) {
                    CompareInfo.status = true;
                    CompareInfo.ok = CompareInfo.count = PlayLists[index].items.Count; 
                    return true;
                }

                CompareInfo.count = PlayLists[index].items.Count;
                CompareInfo.ok = 0;

                int[] found = new int[PlayLists[index].items.Count];
                ReadOnlyCollection<Songs.CSong> allSongs = CBase.Songs.GetSongs();
                for (int i = 0; i < PlayLists[index].items.Count; i++)
                {
                    found[i] = 0;

                    var song = allSongs.FirstOrDefault(item => item.Artist.Equals(PlayLists[index].items[i].artist, StringComparison.OrdinalIgnoreCase) && item.Title.Equals(PlayLists[index].items[i].title, StringComparison.OrdinalIgnoreCase));
                    if (song != null) { 
                        string hash = String.Empty;
                        if (String.IsNullOrEmpty(song.FileHash))
                        {
                            hash = CCommunity.hashTextFile(song.Folder, song.FileName);
                        }
                        else
                        {
                            hash = song.FileHash.ToString();
                        }
                        if (hash == PlayLists[index].items[i].hash){
                            found[i] = 1;
                            _SetPlaylistSongId(index, i, song.ID);
                            CompareInfo.ok++;
                        }
                        else
                        {
                            found[i] = -1;
                            _SetPlaylistSongId(index, i, song.ID);
                            if (CompareInfo.wrongHashes == null)
                                CompareInfo.wrongHashes = new List<SComResultCPlaylistItem>();
                            CompareInfo.wrongHashes.Add(PlayLists[index].items[i]);
                        }
                    }
                    else
                    {
                        if (CompareInfo.missing == null)
                            CompareInfo.missing = new List<SComResultCPlaylistItem>();

                        CompareInfo.missing.Add(PlayLists[index].items[i]);
                    }
                }

                if (CompareInfo.ok != CompareInfo.count)
                {
                   /* for (int z = 0; z < found.Length; z++)
                    {
                        //wrong hash...
                        if (found[z] == -1)
                        {
                            CompareInfo.wrongHashes.Add(PlayLists[index].items[z]);
                        }
                        //missing file ...
                        else if (found[z] != 1)
                        {
                            CompareInfo.missing.Add(PlayLists[index].items[z]);
                        }
                    }*/
                    return false;
                }
                else
                {
                    CompareInfo.status = true;
                    _SetPlaylistReadyStatus(index, true);
                    return true;
                }
            }
            return false;
        }
    }
}
