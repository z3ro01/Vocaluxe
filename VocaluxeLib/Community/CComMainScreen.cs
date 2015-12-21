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
using System.Text;
using System.Drawing;
using System.Net;
using System.Web;
using System.IO;
using System.Threading.Tasks;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;


namespace VocaluxeLib.Community
{
    public class CComMainScreen
    {
        private SComNewsResult news;
        private CStatic Area;
        private CText SongTitle;

        private int itemCount;

        private class SComMScreenTilesData
        {
            public int newsId;
            public string coverUrl;
            public Boolean coverLoaded;
            public string artist;
            public int status;
            public int songId;
            public float maxAlpha;
            public Boolean songUpdated = false;
            public CTextureRef Texture;
        }

        private List<CStatic> _Tiles;
        private List<SComMScreenTilesData> _TileHelpers;
        private Dictionary<String,Bitmap> bitmapCache;

        private float areaWidth;
        private float areaHeight;
        private int scrollPos = 0;

        //TODO: put this to theme xml
        private int cols = 5;
        private int rows = 4;
        private int colspacing = 0;
        private int rowspacing = 0;
        private int tileWidth;
        private int tileHeight;

        private CTextureRef _CoverBGTexture;
        private bool _needUpdate = false;
        public bool _isReady = false;
        private bool _displayReady = false;
        private bool _dataReady = false;

        public int SelectedIndex { get; private set; }
        private int _selectedTile;


        public CComMainScreen(CStatic area, CText st)
        {
            SongTitle = st;
            Area = area;
            area.Visible = false;
            areaWidth = area.W;
            areaHeight = area.H;

            tileWidth = (int)((areaWidth - colspacing * (cols - 1)) / cols);
            tileHeight = (int)((areaHeight - rowspacing * (rows - 1)) / rows);

            bitmapCache = new Dictionary<string, Bitmap>();
        }

        public void LoadTheme(string xmlPath)
        {

        }

        public void ReloadTheme(string xmlPath)
        {
            areaWidth = Area.W;
            areaHeight = Area.H;

            tileWidth = (int)((areaWidth - colspacing * (cols - 1)) / cols);
            tileHeight = (int)((areaHeight - rowspacing * (rows - 1)) / rows);

            OnLoggedIn();
        }


        public void OnLoggedIn()
        {
            if (news.items == null || news.items.Length == 0)
            {
                CCommunity.getNewsAsync(delegate(SComNewsResult data)
                {
                    if (data.items != null && data.items.Length > 0)
                    {
                        news = data;
                        itemCount = data.items.Length;

                        _TileHelpers = new List<SComMScreenTilesData>();
                        for (int x = 0; x < news.items.Length; x++)
                        {
                            var tt = new SComMScreenTilesData();
                            tt.newsId = x;
                            tt.artist = news.items[x].artist;

                            if (news.items[x].coverUrl != null)
                            {
                                tt.coverUrl = news.items[x].coverUrl;
                                tt.coverLoaded = false;
                            }
                            else
                            {
                                tt.Texture = CBase.Cover.GenerateCover(news.items[x].title, ECoverGeneratorType.Artist, null);
                                tt.coverLoaded = true;
                            }

                            tt.status = -1;
                            tt.songId = -1;
                            tt.maxAlpha = 1;
                            _TileHelpers.Add(tt);
                        }

                        _dataReady = true;
                        //load covers in background
                        var task = Task.Factory.StartNew(() => loadCoverTextures());
                        if (_displayReady == true) {
                            CheckSongStatus();
                            _InitTiles();
                            _UpdateView();
                        }
                    }
                });
            }
        }

        public void SelectTile(int id)
        {
            if (id > -1 && id < _Tiles.Count) {
                _selectedTile = id;
                 _Tiles[id].Selected = true;
                var newsId = (scrollPos * cols) + id;
                if (newsId > -1 && newsId < news.items.Length)
                {
                    SongTitle.Text = news.items[newsId].artist + " - " + news.items[newsId].title;
                }
            }else {
                _selectedTile = -1;
                SongTitle.Text = "";
            }
        }

      

        public void OpenTileInfo(int id)
        {
            var newsId = (scrollPos * cols) + id;
            if (newsId > -1)
            {
                var d = _TileHelpers.FirstOrDefault(x => x.newsId == newsId);
                if (d != null)
                {
                    if (d.status == 0)
                    {
                        SongIsUptodate(d.newsId, d.songId);
                    }
                    else if (d.status == 1)
                    {
                        NewSong(d.newsId);
                    }else if (d.status == 2) {
                        UpdateSong(d.newsId, d.songId);
                    }
                }
            }
        }

        public bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_isReady) {
                if (CHelper.IsInBounds(Area.Rect, mouseEvent))
                {
                    int foundTile = -1;
                    for (int i = 0; i < _Tiles.Count; i++)
                    {
                        if (foundTile == -1)
                        {
                            if (CHelper.IsInBounds(_Tiles[i].Rect, mouseEvent))
                            {
                                foundTile = i;
                            }
                            else
                            {
                                _Tiles[i].Selected = false;
                            }
                        }
                        else
                        {
                            _Tiles[i].Selected = false;
                        }
                    }
                    SelectTile(foundTile);

                    //click
                    if (mouseEvent.LB && _selectedTile > -1)
                    {
                        OpenTileInfo(_selectedTile);
                    }
                }
                else
                {
                    SelectTile(-1);
                    for (int i = 0; i < _Tiles.Count; i++)
                    {
                        _Tiles[i].Selected = false;
                    }
                }
            }

            return false;
        }

        public void OnShow()
        {
            _displayReady = true;
            if (_dataReady && !_isReady)
            {
                _InitTiles();
                _UpdateView();
                CheckSongStatus();
            }
        }

        public void UpdateGame()
        {

        }

        public void Draw()
        {
            if (news.items != null && news.items.Length > 0 && _Tiles != null)
            {
                if (_needUpdate)
                {
                    _UpdateView();
                    _needUpdate = false;
                }

                int x = scrollPos * cols;
                foreach (CStatic tile in _Tiles)
                {
                    if (tile.Visible) {
                        if (_TileHelpers[x] != null)
                        {
                            if (tile.Alpha < _TileHelpers[x].maxAlpha)
                            {
                                tile.Alpha += 0.01f;
                                if (tile.Alpha > _TileHelpers[x].maxAlpha)
                                {
                                    tile.Alpha = _TileHelpers[x].maxAlpha;
                                }
                            }
                        }

                        if (tile.Selected) {
                            tile.Alpha = 1;
                            tile.Draw(EAspect.Crop, 1.2f, -0.1f);
                        }
                        else
                        {
                            if (tile.Alpha == 1) { tile.Alpha = _TileHelpers[x].maxAlpha;  }
                            EAspect aspect = EAspect.Stretch;
                            tile.Draw(aspect);
                        }
                    }
                x++;
                }
            }
        }

        public void Update(SComNewsResult data)
        {
            news = data;
        }

      
        private void _InitTiles()
        {
            _CoverBGTexture = CBase.Themes.GetSkinTexture("CoverTile", 0);
            SColorF _Color = new SColorF(1f, 1f, 1f, 1f);
            _Tiles = new List<CStatic>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    CStatic tile;
                    var rect = new SRectF(Area.X + j * (tileWidth + colspacing), Area.Y + i * (tileHeight + rowspacing), tileWidth, tileHeight, Area.Z);
                    var texture = _TileHelpers[i + j];
                    if (texture != null && texture.Texture != null)
                    {
                        tile = new CStatic(0, _TileHelpers[i + j].Texture, _Color, rect);
                    }
                    else
                    {
                        tile = new CStatic(0, _CoverBGTexture, _Color, rect);
                    }

                    tile.Alpha = 0f;
                    _Tiles.Add(tile);
                }
            }
            _isReady = true;
        }

        private void _UpdateView()
        {
            int dispFrom = scrollPos*cols;
            int dispTo = dispFrom+(rows*cols);

            int t = 0;
            for (int i = dispFrom; i < dispTo; i++)
            {
                if (news.items.Length < i)
                {
                    _Tiles[t].Visible = false;
                }
                else
                {
                    if (_TileHelpers[i] != null)
                    {
                        if (_Tiles[t].Texture == _CoverBGTexture || (_TileHelpers[i].Texture != null && _Tiles[t].Texture != _TileHelpers[i].Texture))
                        {
                            if (_TileHelpers[i].coverLoaded == true)
                            {
                                _Tiles[t].Texture = _TileHelpers[i].Texture;
                                _Tiles[t].Visible = true;
                            }
                            else
                            {
                                _Tiles[t].Visible = false;
                            }
                        }
                    }
                }
                t++;
            }
        }

        private void CheckSongStatus()
        {
            if (news.items.Length > 0 && _TileHelpers.Count > 0)
            {
                for (var x = 0; x < news.items.Length; x++)
                {
                    int songId = CComSong.FindSongByHash(news.items[x].txtHash);
                    if (songId == -1)
                    {
                        if (news.items[x].knownHashes != null)
                        {
                            songId = CComSong.FindSongByHash(news.items[x].knownHashes);
                            if (songId > -1)
                            {
                                _TileHelpers[x].songId = songId;
                                _TileHelpers[x].status = 2;
                            }
                            else { _TileHelpers[x].status = 1; }
                        }
                    }
                    else if (songId > -1)
                    {
                        _TileHelpers[x].songId = songId;
                        _TileHelpers[x].status = 0;
                        _TileHelpers[x].maxAlpha = 0.2f;

                    }
                }
            }
        }

        private void UpdateSongStatus(int id)
        {
            var d = _TileHelpers.FirstOrDefault(x => x.newsId == id);
            if (d != null)
            {
                int songId = CComSong.FindSongByHash(news.items[id].txtHash);
                if (songId > 0)
                {
                    d.status = 0;
                    d.songId = songId;
                    d.maxAlpha = 0.2f;
                }
            }
        }

        #region popups
        private void UpdateSong(int id, int songId)
        {
            CPopupHelper.Confirm("TR_COMMUNITY_SONGUPDATEFOUND", "TR_COMMUNITY_SONGUPDATEFOUND_TEXT", delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.Name.Equals("onKeyEscape")
                    || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                    || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                )
                {
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                  || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
               )
                {
                    var txtFile = news.items[id].fileList.FirstOrDefault(x => x.Key == "txt").Value;
                    if (txtFile != null)
                    {
                        var txtUrl = txtFile.FirstOrDefault(x => x.Key == "url").Value;
                        if (txtUrl != null)
                        {
                            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                            CComSong.UpdateSongTextFile(txtUrl, songId, delegate(bool status)
                            {
                                if (status) {
                                    UpdateSongStatus(id);
                                }
                            });
                            return;
                        }
                    }
                   
                   CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_ERROR_UPDATETXT0");
                }
            });
        }

        private void SongIsUptodate(int id, int songId)
        {
            CPopupHelper.Confirm("TR_COMMUNITY_SONGISUPTODATE", "TR_COMMUNITY_SONGISUPTODATE_TEXT", delegate(SPopupGeneralEvent eventData)
            {
                if (eventData.Name.Equals("onKeyEscape")
                    || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                    || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                )
                {
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                }
                if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                   || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                )
                {
                    CBase.Game.Reset();
                    CBase.Game.ClearSongs();
                    var song = CBase.Songs.GetSongByID(songId);
                    EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                    if (song.IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;

                    CBase.Game.AddSong(songId, gm);
                    CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    CBase.Graphics.FadeTo(EScreen.Names);
                }
            });

            var popup = CBase.Graphics.GetPopup(EPopupScreens.PopupGeneral);
            var data = popup.GetDisplayData();
            data.ButtonYesLabel = CBase.Language.Translate("TR_COMMUNITY_SINGIT");
            popup.SetDisplayData(data);
        }

        private void NewSong(int id)
        {
            if (news.items[id].hasDownloadSupport)
            {
                CPopupHelper.Confirm("TR_COMMUNITY_DLSONG", "TR_COMMUNITY_DLSONGTEXT", delegate(SPopupGeneralEvent eventData)
                {
                    if (eventData.Name.Equals("onKeyEscape")
                        || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                    )
                    {
                        CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                    }
                    if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                        || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                    )
                    {
                        _DownloadSongHelper(id);
                    }
                });
            }
            else
            {
                CPopupHelper.Alert("TR_COMMUNITY_DLSONG", "TR_COMMUNITY_ERROR_NODLSUPPORT");
            }
        }
        #endregion

        private void _DownloadSongHelper(int id)
        {
            if (!String.IsNullOrWhiteSpace(news.items[id].licenceUrl))
            {
                CPopupHelper.Loading("TR_COMMUNITY_LOADING", "");
                CCommunity.getTextAsync(news.items[id].licenceUrl, delegate(string response)
                {
                    if (!String.IsNullOrWhiteSpace(response))
                    {
                        SPopupGeneral data = new SPopupGeneral();
                        data.ButtonYesLabel = CBase.Language.Translate("TR_COMMUNITY_ACCEPT");
                        data.ButtonNoLabel = CBase.Language.Translate("TR_COMMUNITY_REJECT");
                        data.Size = EPopupGeneralSize.Big;

                        CPopupHelper.Confirm("TR_COMMUNITY_LICENCE", response, delegate(SPopupGeneralEvent eventData) {
                            if (eventData.Name.Equals("onKeyEscape")
                                || (eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonNo"))
                                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonNo"))
                            )
                            {
                                CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
                            }
                            if ((eventData.Name.Equals("onKeyReturn") && eventData.Target.Equals("ButtonYes"))
                                || (eventData.Name.Equals("onMouseLB") && eventData.Target.Equals("ButtonYes"))
                            )
                            {
                                _DownloadSelectedSong(id);
                            }
                        }, data);

                    }
                    else
                    {
                        CPopupHelper.Alert("TR_COMMUNITY_ERROR", "TR_COMMUNITY_UNKERR");
                    }
                });
            }
            //download
            else
            {
                _DownloadSelectedSong(id);
            }
        }

        private void _DownloadSelectedSong(int id)
        {
            CBase.Graphics.HidePopup(EPopupScreens.PopupGeneral);
            CComSong.DownloadSong(news.items[id], delegate(bool status)
            {
                if (status == true)
                    CheckSongStatus();
            });
        }

        #region bitmap downloading

        private void loadCoverTextures()
        {
            for (int i = 0; i < _TileHelpers.Count; i++)
            {
                if (_TileHelpers[i].coverLoaded == false)
                {
                    _TileHelpers[i].coverLoaded = true;
                    if (_TileHelpers[i].coverUrl != null)
                    {
                        var md5Hash = CCommunity.MD5Hash(_TileHelpers[i].coverUrl);
                        var bmp = getBitmapFromUrl(_TileHelpers[i].coverUrl);
                        if (bmp == null || bmp.GetSize() == null)
                        {
                            _TileHelpers[i].Texture = CBase.Cover.GenerateCover(_TileHelpers[i].artist, ECoverGeneratorType.Artist, null);
                        }
                        else
                        {
                            _TileHelpers[i].Texture = CBase.Cover.GenerateCoverFromBitmap(md5Hash, ECoverGeneratorType.Artist, bmp);
                        }
                        _needUpdate = true;
                    }
                }
            }
        }

        private Bitmap getBitmapFromUrl(string url)
        {
            var md5Hash = CCommunity.MD5Hash(url);
            Bitmap cachedBmp;
            bitmapCache.TryGetValue(md5Hash, out cachedBmp);
            if (cachedBmp != null)
            {
                return cachedBmp;
            }
            var bmp = _loadBitmapFromUrl(url);
            if (bmp != null)
            {
                if (!bitmapCache.ContainsKey(md5Hash))
                {
                    bitmapCache.Add(md5Hash, bmp);
                }
            }
            return bmp;
        }

        private Bitmap _loadBitmapFromUrl(string url)
        {
            Bitmap bmp = null;
            Stream stream = null;
            HttpWebResponse response = null;
            try
            {

                HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);
                webreq.AllowWriteStreamBuffering = true;
                webreq.AllowAutoRedirect = true;
                response = (HttpWebResponse)webreq.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if ((stream = response.GetResponseStream()) != null)
                    {
                        var bitmap = new Bitmap(stream);
                        if (bitmap.Height > 0 && bitmap.Width > 0)
                        {
                            bmp = bitmap;
                        }
                    }
                }
            }
            catch (Exception e)
            { }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (response != null)
                    response.Close();
            }

            return bmp;
        }

        #endregion

    }
}
