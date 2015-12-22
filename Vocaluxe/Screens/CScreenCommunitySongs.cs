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
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Draw;
using VocaluxeLib.Community;

namespace Vocaluxe.Screens
{
    class CScreenCommunitySongs : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
		}

        private class STileHelper
        {
            public int dataId;
            public bool changed;
            public bool textureLoaded;
        }

        private const string _StaticSongsArea = "SongsArea";
        private const string _SelectSlideOrder = "SelectSlideOrder";

        private string[] _Orders;
        private int _CurrentOrder = 0;
        private bool _DataUpdated = false;
        private bool _QueryChanged = false;

        private List<SComRemoteSong> _Data;
        private List<STileHelper> _TileHelper;
        private SComResultListInfo _Listinfo = new SComResultListInfo();
        private List<CStatic> _Tiles;
        private CTextureRef _DefaultTexture;

        public override void Init()
        {
            base.Init();
            _ThemeSelectSlides = new string[] { _SelectSlideOrder };
            _ThemeStatics = new string[] { _StaticSongsArea };

            _Orders = new string[5];
            _Orders[0] = "Upload date";
            _Orders[1] = "Artist";
            _Orders[2] = "Title";
            _Orders[3] = "Modification date";
            _Orders[4] = "Comments";

            _Data = new List<SComRemoteSong>();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlideOrder].AddValues(_Orders);
            _SelectSlides[_SelectSlideOrder].Selection = _CurrentOrder;

            _InitTiles();
        }

        public override void Draw()
        {
            base.Draw();
            int i = 0;
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Visible)
                {
                    if (tile.Texture != _DefaultTexture && tile.Alpha < 1)
                    {
                        var alpha = tile.Alpha;
                        alpha += 0.1f;
                        tile.Alpha = alpha.Clamp( 0f, 1f);
                    }
                    else if (tile.Texture == _DefaultTexture && tile.Alpha > 0.1f)
                    {
                        var alpha = tile.Alpha;
                        alpha -= 0.1f;
                        tile.Alpha = alpha.Clamp(0f, 1f);
                    }

                    if (tile.Selected)
                    {
                        tile.Alpha = 1;
                        tile.Draw(EAspect.Crop, 1.2f, -0.1f);
                    }
                    else
                    {
                        EAspect aspect = EAspect.Stretch;
                        tile.Draw(aspect);
                    }
                }
            }
        }

        public override void OnShow()
        {
            _Load();
            base.OnShow();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (CHelper.IsInBounds(_Statics[_StaticSongsArea].Rect, mouseEvent))
            {
                int foundTile = -1;
                for (int i = 0; i < _Tiles.Count; i++)
                {
                    if (foundTile == -1)
                    {
                        if (CHelper.IsInBounds(_Tiles[i].Rect, mouseEvent))
                        {
                            foundTile = i;
                            _Tiles[i].Selected = true;
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
                
                //click
                if (mouseEvent.LB && foundTile > -1)
                {
                    //foundTile
                    //OpenTileInfo(_selectedTile);
                }
            }
            else
            {
                for (int i = 0; i < _Tiles.Count; i++)
                {
                    _Tiles[i].Selected = false;
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            if (_DataUpdated)
            {
                for (int i = 0; i < _Data.Count; i++)
                {
                    var helper = new STileHelper();
                    if (i >= 0 && i < _Tiles.Count) {
                        helper.dataId = i;

                        if (!String.IsNullOrWhiteSpace(_Data[i].coverUrl)) {
                            CTextureLoader.LoadTo(_Data[i].coverUrl, _Tiles[i], delegate(bool status, bool fromcache, CStatic element)
                            {
                                if (!status)
                                {
                                    element.Texture = CBase.Cover.GenerateCover(_Data[i].artist, ECoverGeneratorType.Artist, null);
                                }
                            });
                        }
                        else
                        {
                            _Tiles[i].Texture = CBase.Cover.GenerateCover(_Data[i].artist, ECoverGeneratorType.Artist, null);
                            _TileHelper[i] = helper;
                        }
                    }
                   
                    
                }
                _DataUpdated = false;
            }
            return true;
        }

        private void _InitTiles()
        {
            _TileHelper = new List<STileHelper>();

            int rows = 5;
            int cols = 10;
            int rowspacing = 5;
            int colspacing = 5;

            int tileWidth = (int)((_Statics[_StaticSongsArea].W - colspacing * (cols - 1)) / cols);
            int tileHeight = (int)((_Statics[_StaticSongsArea].H - rowspacing * (rows - 1)) / rows);

            _DefaultTexture = _Statics[_StaticSongsArea].Texture;  
            SColorF _Color = new SColorF(1f, 1f, 1f, 1f);
            _Tiles = new List<CStatic>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    CStatic tile;
                    var rect = new SRectF(_Statics[_StaticSongsArea].X + j * (tileWidth + colspacing), _Statics[_StaticSongsArea].Y + i * (tileHeight + rowspacing), tileWidth, tileHeight, _Statics[_StaticSongsArea].Z);
                    tile = new CStatic(0, _DefaultTexture, _Color, rect);
                    tile.Alpha = 0.1f;
                    _Tiles.Add(tile);
                    _TileHelper.Add(new STileHelper());
                }
            }

            _Statics[_StaticSongsArea].Visible = false;
        }

        private void _Load()
        {
            if (_QueryChanged)
                _Data.Clear();

            var query = new SComQueryCmd();
            query.method = "getsongs";
            query.parameters = new Dictionary<string,string>();
            query.parameters.Add("orderby", _CurrentOrder.ToString());
            query.parameters.Add("start",    (_Listinfo.end+1).ToString());

            CCommunity.getSongsAsync(query, delegate(SComSongsResult data)
            {
                if (data.status == 1)
                {
                    Console.WriteLine("Data is here");
                    for (int i = 0; i < data.items.Length; i++)
                    {
                        _Data.Add(data.items[i]);
                    }
                    _DataUpdated = true;
                }
                else
                {
                    Console.WriteLine("Data status is 0");
                }
            });
        }
    }
}
