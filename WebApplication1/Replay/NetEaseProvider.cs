using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Replay
{
    public sealed class NetEaseProvider : IMusicProvider
    {
        class HttpRequest : HttpHelper<NetEaseProvider> { }

        static NetEaseProvider()
        {
            HttpRequest.HttpRegister(null, TimeSpan.FromSeconds(20), contentType: "application/x-www-form-urlencoded", keepAlive: false, headers: new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Encoding", "gzip,deflate,sdch"},
                {"Accept-Language", "zh-CN,zh;q=0.8,gl;q=0.6,zh-TW;q=0.4" },
                { "Host", "music.163.com"},
                { "Referer", "http://music.163.com/search/"},
                {"User-Agent","Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.152 Safari/537.36" }
            });
        }
        private static readonly string modulus = "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b72" +
                                                "5152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbd" +
                                                "a92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe48" +
                                                "75d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7";
        private static readonly byte[] nonce = Encoding.UTF8.GetBytes("0CoJUm6Qyw8W8jud");
        private static readonly string pubkey = "010001";
        private static readonly byte[] iv = Encoding.UTF8.GetBytes("0102030405060708");
        private static readonly byte[] choices = Encoding.ASCII.GetBytes("012345679abcdef");
        #region 辅助方法
        private string EncryptedId(string id)
        {
            byte[] magic = Encoding.UTF8.GetBytes("3go8&$8*3*3h0k(2)2");
            byte[] songId = Encoding.UTF8.GetBytes(id);
            int magicLen = magic.Length;
            for (int i = 0; i < songId.Length; i++)
            {
                songId[i] = (byte)(songId[i] ^ magic[i % magicLen]);
            }
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                return Convert.ToBase64String(md5.ComputeHash(songId)).Replace('/', '_').Replace('+', '-');
            }
        }
        private byte[] CreateSecretKey(int size)
        {
            var key = new byte[size];
            Random r = new Random();
            for (int i = 0; i < size; i++)
            {
                key[i] = choices[r.Next(0, choices.Length)];
            }
            return key;
        }
        private string AesEncrypt(string text, byte[] secKey)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            int pad = 16 - buffer.Length % 16;
            buffer = buffer.Concat(Enumerable.Repeat((byte)pad, pad)).ToArray();
            byte[] cipherBytes = null;
            using (var aes = Aes.Create())
            {
                aes.Key = secKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(buffer, 0, buffer.Length);
                        cipherBytes = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(cipherBytes.ToArray());

        }
        private string RsaEncrypt(byte[] text, string pubKey, string modulus)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var x in text.Reverse())
            {
                sb.Append(x.ToString("x2"));
            }
            var a1 = BigInteger.Parse(sb.ToString(), System.Globalization.NumberStyles.HexNumber);
            var a2 = BigInteger.Parse(pubKey, System.Globalization.NumberStyles.HexNumber);
            var a3 = BigInteger.Parse(modulus, System.Globalization.NumberStyles.HexNumber);
            var rs = BigInteger.ModPow(a1, a2, a3);
            return rs.ToString("x2").TrimStart('0').PadLeft(256, '0');
        }
        private string EncryptedRequest(object token)
        {
            string text = JsonConvert.SerializeObject(token);
            byte[] secKey = CreateSecretKey(16);
            string encText = AesEncrypt(AesEncrypt(text, nonce), secKey);
            string EncSecKey = RsaEncrypt(secKey, pubkey, modulus);
            return $"params={Uri.EscapeDataString(encText)}&encSecKey={EncSecKey}";
        }
        private async Task<object> TopPlayList(string category = "全部", string order = "hot", int offset = 0, int limit = 60)
        {
            category = Uri.EscapeDataString(category);
            string action = $"http://music.163.com/api/playlist/list?cat={category}&order={order}&offset={offset}&total={(offset > 0 ? true : false)}&limit={limit}";
            var message = await HttpRequest.SendAsync(action);
            if (message.Content != null)
            {
                dynamic data = JsonConvert.DeserializeObject(message.Content);
                return data["playlists"];
            }
            return null;
        }
        private SongViewModel ConvertSong(dynamic song)
        {
            SongViewModel d = new SongViewModel();
            d.Id = $"netrack_{song["id"]}";
            d.Title = song["name"];
            d.Artist = song["artists"][0]["name"];
            d.ArtistId = $"neartist_{song["artists"][0]["id"]}";
            d.Album = song["album"]["name"];
            d.AlbumId = $"nealbum_{song["album"]["id"]}";
            d.Source = "netease";
            d.SourceUrl = $"http://music.163.com/#/song?id={song["id"]}";
            if (song["album"]["picUrl"] != null)
            {
                d.ImgUrl = song["album"]["picUrl"];
            }
            else
                d.ImgUrl = "";
            d.Url = $"/api/playlist/trackfile?{d}";
            return d;
        }
        private SongViewModel ConvertSong2(dynamic song)
        {
            SongViewModel d = new SongViewModel();
            d.Id = $"netrack_{song["id"]}";
            d.Title = song["name"];
            d.Artist = song["ar"][0]["name"];
            d.ArtistId = $"neartist_{song["ar"][0]["id"]}";
            d.Album = song["al"]["name"];
            d.AlbumId = $"nealbum_{song["al"]["id"]}";
            d.Source = "netease";
            d.SourceUrl = $"http://music.163.com/#/song?id={song["id"]}";
            if (song["al"]["picUrl"] != null)
            {
                d.ImgUrl = song["al"]["picUrl"];
            }
            else
                d.ImgUrl = "";
            d.Url = $"/api/playlist/trackfile?{d}";
            return d;
        }
        #endregion

        public string FileType { get => ".mp3"; }


        public async Task<PlayListViewModel> GetAlbum(string albumId)
        {
            string url = $"http://music.163.com/api/album/{albumId}/";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = data["album"]["picUrl"],
                    Title = data["album"]["name"],
                    Id = $"nealbum_{albumId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in data["album"]["songs"])
                {
                    if (song["status"] == -1)
                        continue;
                    result.Add(ConvertSong(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<PlayListViewModel> GetArtist(string artistId)
        {
            string url = $"http://music.163.com/api/artist/{artistId}";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = data["artist"]["picUrl"],
                    Title = data["artist"]["name"],
                    Id = $"neartist_{artistId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in data["hotSongs"])
                {
                    if (song["status"] == -1)
                        continue;
                    result.Add(ConvertSong(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<PlayListViewModel> GetPlayList(string playListId)
        {
            var d = new
            {
                id = playListId,
                offset = 0,
                total = true,
                limit = 1000,
                n = 1000,
                csrf_token = ""
            };
            string url = $"http://music.163.com/weapi/v3/playlist/detail";
            var response = await HttpRequest.PostAsync(url, EncryptedRequest(d));
            if (response.Content != null)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = data["playlist"]["coverImgUrl"],
                    Title = data["playlist"]["name"],
                    Id = $"neplaylist_{playListId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in data["playlist"]["tracks"])
                {
                    if (song["status"] == -1)
                        continue;
                    result.Add(ConvertSong2(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;

        }

        public async Task<List<PlayListInfoViewModel>> GetPlayListInfos(int offset)
        {
            dynamic lists = await TopPlayList(offset: offset);
            if (lists == null)
                return null;
            var result = new List<PlayListInfoViewModel>();
            foreach (var l in lists)
            {
                var d = new PlayListInfoViewModel
                {
                    CoverImgUrl = l["coverImgUrl"],
                    Title = l["name"],
                    PlayCount = l["playCount"],
                    ListId = $"neplaylist_{l["id"]}"
                };
                result.Add(d);
            }
            return result;
        }

        public async Task<string> GetUrlById(string songId)
        {
            var d = new
            {
                ids = new long[] { long.Parse(songId) },
                br = 12800,
                csrf_token = ""
            };
            string url = "http://music.163.com/weapi/song/enhance/player/url?csrf_token=";
            var response = await HttpRequest.PostAsync(url, EncryptedRequest(d));
            if (!string.IsNullOrEmpty(response.Content))
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                return data["data"][0]["url"];
            }
            return null;
        }

        public async Task<List<SongViewModel>> SearchTrack(string keyword)
        {
            string searchUrl = "http://music.163.com/api/search/pc";
            var response = await HttpRequest.PostAsync(searchUrl, $"s={keyword}&type=1&offset=0&total=true&limit=60");
            List<SongViewModel> result = null;
            if (response.Content != null)
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                result = new List<SongViewModel>();
                foreach (var song in data["result"]["songs"])
                {
                    if (song["status"] == -1)
                        continue;
                    result.Add(ConvertSong(song));
                }
            }
            return result;
        }

        public async Task<string> GetLyric(string trackId, string lyricUrl = null)
        {
            var targetUrl = "http://music.163.com/weapi/song/lyric?csrf_token=";
            var d = new
            {
                id = trackId,
                lv = -1,
                tv = -1,
                csrf_token = ""
            };
            var response = await HttpRequest.PostAsync(targetUrl, EncryptedRequest(d));
            if (!string.IsNullOrEmpty(response.Content))
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                if (data["lrc"] != null)
                    return data["lrc"]["lyric"];
            }
            return null;
        }
    }
}
