using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Community;

namespace Vocaluxe.Base
{
    public static class CTextureLoader
    {
        private static Dictionary<String, Bitmap> _BitmapCache = new Dictionary<string,Bitmap>();
        private static Dictionary<String, CTextureRef> _TextureCache = new Dictionary<string,CTextureRef>();
        public static CTextureRef EmptyTexture { get; private set; }

        public static void LoadTo(string url, CStatic element, Action<bool, bool, CStatic> callback = null)
        {
            var md5Hash = CCommunity.MD5Hash(url);
            CTextureRef cachedTexture;
            _TextureCache.TryGetValue(md5Hash, out cachedTexture);
            if (cachedTexture != null)
            {
                element.Texture = cachedTexture;
                if (callback != null)
                    callback(true, true, element);
            }
            else { 
                Task.Factory.StartNew(() => _Load(url, delegate(Bitmap bmp){
                    if (bmp != null)
                    {
                        CTextureRef texture = CDraw.EnqueueTexture(bmp);
                        element.Texture = texture;
                        if (!_TextureCache.ContainsKey(md5Hash))
                        {
                            _TextureCache.Add(md5Hash, texture);
                        }
                        if (callback != null)
                            callback(true, false, element);
                    }
                    else
                    {
                        if (callback != null)
                            callback(false, false, element);
                    }
                },false));
            }
        }

        public static void Load(string url, Action<Bitmap> callback)
        {
            Task.Factory.StartNew(() => _Load(url, callback, true));
        }

        public static void ClearCache()
        {
            _BitmapCache.Clear();
            _TextureCache.Clear();
        }

        private static void _Load(string url, Action<Bitmap> callback, bool usecache = true)
        {
            var bmp = GetBitmapFromUrl(url, usecache);
            callback(bmp);
        }

        #region bitmap downloading

        private static Bitmap GetBitmapFromUrl(string url, bool usecache = true)
        {
            var md5Hash = CCommunity.MD5Hash(url);
            Bitmap cachedBmp;
            _BitmapCache.TryGetValue(md5Hash, out cachedBmp);
            if (cachedBmp != null)
            {
                return cachedBmp;
            }
            var bmp = LoadBitmapFromUrl(url);

            if (usecache)
            {
                if (bmp != null)
                {
                    if (!_BitmapCache.ContainsKey(md5Hash))
                    {
                        _BitmapCache.Add(md5Hash, bmp);
                    }
                }
            }
            return bmp;
        }

        private static Bitmap LoadBitmapFromUrl(string url)
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
