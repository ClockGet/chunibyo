using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Replay
{
    public class XiaMiProvider : IMusicProvider
    {
        class HttpRequest : HttpHelper<XiaMiProvider> { }
        private static HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        static XiaMiProvider()
        {
            HttpRequest.HttpRegister(null, TimeSpan.FromSeconds(10), contentType: "application/x-www-form-urlencoded", keepAlive: true, headers: new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Encoding", "gzip,deflate,sdch"},
                {"Accept-Language", "zh-CN,zh;q=0.8,gl;q=0.6,zh-TW;q=0.4" },
                { "Host", "api.xiami.com"},
                { "Referer", "http://m.xiami.com/"},
                {"User-Agent","Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.152 Safari/537.36" }
            });
        }
        private Regex reg = new Regex(@"\<\d+\>");
        #region 辅助方法
        private string Caesar(string location)
        {
            int num = int.Parse(location.Substring(0, 1));
            int avgLen = Convert.ToInt32((location.Length - 1) / num);
            int remainder = Convert.ToInt32((location.Length - 1) % num);
            string[] result = new string[num];
            for (int i = 0; i < remainder; i++)
            {
                int start = i * (avgLen + 1) + 1;
                int stop = (i + 1) * (avgLen + 1) + 1;
                int len = stop - start;
                result[i] = location.Substring(start, len);
            }
            string temp = location.Substring((avgLen + 1) * remainder);
            for (int i = remainder; i < num; i++)
            {
                int j = i - remainder;
                int start = j * avgLen + 1;
                int stop = (j + 1) * avgLen + 1;
                result[i] = temp.Substring(start, stop - start);
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < avgLen; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    sb.Append(result[j][i]);
                }
            }
            for (int r = 0; r < remainder; r++)
            {
                sb.Append(result[r].Last());
            }
            return Uri.UnescapeDataString(sb.ToString()).Replace('^', '0');
        }
        private SongViewModel ConvertSong(dynamic song)
        {
            SongViewModel d = new SongViewModel();
            d.Id = $"xmtrack_{song["song_id"]}";
            d.Title = song["song_name"];
            d.Artist = song["artist_name"];
            d.ArtistId = $"xmartist_{song["artist_id"]}";
            d.Album = song["album_name"];
            d.AlbumId = $"xmalbum_{song["album_id"]}";
            d.Source = "xiami";
            d.SourceUrl = $"http://www.xiami.com/song/{song["song_id"]}";
            if (song["album_logo"] !=null)
            {
                d.ImgUrl = song["album_logo"];
            }
            else
            {
                d.ImgUrl = "";
            }
            d.Url = $"/api/playlist/trackfile?{d}";
            if(song["lyric"] !=null)
            {
                d.LyricUrl = song["lyric"];
            }
            return d;
        }
        private string RetinaUrl(string s)
        {
            return s.Substring(0, s.Length - 6) + s.Substring(s.Length - 4);
        }
        private string SplitString(string source, string startMark, string stopMark)
        {
            var s = source.IndexOf(startMark);
            var e = source.LastIndexOf(stopMark);
            var jc = source.Substring(s + startMark.Length, e - (s + startMark.Length));
            return jc;
        }
        #endregion
        public string FileType { get => ".mp3"; }

        public async Task<PlayListViewModel> GetAlbum(string albumId)
        {
            string url = $"http://api.xiami.com/web?v=2.0&app_key=1&id={albumId}" +
                        "&page=1&limit=20&_ksTS=1459931285956_216" +
                        "&callback=jsonp217&r=album/detail";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                string jsonString = SplitString(response.Content, "jsonp217(", ")");
                dynamic data = JsonConvert.DeserializeObject(jsonString);
                string artistName = data["data"]["artist_name"];
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = RetinaUrl(data["data"]["album_logo"]),
                    Title = data["data"]["album_name"],
                    Id = $"xmalbum_{albumId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in data["data"]["songs"])
                {
                    SongViewModel d = new SongViewModel
                    {
                        Id = $"xmtrack_{song["song_id"]}",
                        Title = song["song_name"],
                        Artist = artistName,
                        ArtistId = $"xmartist_{song["artist_id"]}",
                        Album = song["album_name"],
                        AlbumId = $"xmalbum_{song["album_id"]}",
                        ImgUrl = song["album_logo"],
                        Source = "xiami",
                        SourceUrl = $"http://www.xiami.com/song/{song["song_id"]}"
                    };
                    d.Url = $"/track_file?{d}";
                    result.Add(d);
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<PlayListViewModel> GetArtist(string artistId)
        {
            string url1 = $"http://api.xiami.com/web?v=2.0&app_key=1&id={artistId}&page=1&limit=20&_ksTS=1459931285956_216&callback=jsonp217&r=artist/detail";
            string url2 = $"http://api.xiami.com/web?v=2.0&app_key=1&id={artistId}&page=1&limit=20&_ksTS=1459931285956_216&callback=jsonp217&r=artist/hot-songs";
            var response = await Task.WhenAll(HttpRequest.SendAsync(url1), HttpRequest.SendAsync(url2));
            if (response[0].Content == null || response[1].Content == null)
                return null;
            string jsonString = SplitString(response[0].Content, "jsonp217(", ")");
            dynamic data = JsonConvert.DeserializeObject(jsonString);
            string artistName = data["data"]["artist_name"];
            PlayListInfoViewModel info = new PlayListInfoViewModel
            {
                CoverImgUrl = RetinaUrl(data["data"]["logo"]),
                Title = artistName,
                Id = $"xmartist_{artistId}"
            };

            jsonString = SplitString(response[1].Content, "jsonp217(", ")");
            data = JsonConvert.DeserializeObject(jsonString);
            var result = new List<SongViewModel>();
            foreach (var song in data["data"])
            {
                SongViewModel d = new SongViewModel
                {
                    Id = $"xmtrack_{song["song_id"]}",
                    Title = song["song_name"],
                    Artist = artistName,
                    ArtistId = $"xmartist_{artistId}",
                    Album = "",
                    AlbumId = "",
                    ImgUrl = "",
                    Source = "xiami",
                    SourceUrl = $"http://www.xiami.com/song/{song["song_id"]}"
                };
                d.Url = $"/track_file?{d}";
                result.Add(d);
            }
            return new PlayListViewModel { Info = info, Tracks = result };
        }

        public async Task<PlayListViewModel> GetPlayList(string playListId)
        {
            string url = $"http://api.xiami.com/web?v=2.0&app_key=1&id={playListId}&_ksTS=1459928471147_121&callback=jsonp122&r=collect/detail";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                string jsonString = SplitString(response.Content, "jsonp122(", ")");
                dynamic data = JsonConvert.DeserializeObject(jsonString);
                PlayListInfoViewModel info = new PlayListInfoViewModel();
                string logoUrl = data["data"]["logo"];
                info.CoverImgUrl = logoUrl;
                info.Title = data["data"]["collect_name"];
                info.Id = $"xmplaylist_{playListId}";
                var result = new List<SongViewModel>();
                foreach (var song in data["data"]["songs"])
                {
                    result.Add(ConvertSong(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<List<PlayListInfoViewModel>> GetPlayListInfos(int offset)
        {
            var page = offset / 60 + 1;
            string url = "http://api.xiami.com/web?v=2.0&app_key=1&_ksTS=1459927525542_91" +
                        $"&page={page}&limit=60&callback=jsonp92&r=collect/recommend";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                string jsonString = SplitString(response.Content, "jsonp92(", ")");
                dynamic data = JsonConvert.DeserializeObject(jsonString);
                var result = new List<PlayListInfoViewModel>();
                foreach (var l in data["data"])
                {
                    PlayListInfoViewModel d = new PlayListInfoViewModel
                    {
                        CoverImgUrl = l["logo"],
                        Title = l["collect_name"],
                        PlayCount = 0,
                        ListId = $"xmplaylist_{l["list_id"]}"
                    };
                    result.Add(d);
                }
                return result;
            }
            return null;
        }

        public async Task<string> GetUrlById(string songId)
        {
            string url = $"http://www.xiami.com/song/playlist/id/{songId}/object_name/default/object_id/0/cat/json";
            var response = await HttpRequest.SendAsync(url);
            if (!string.IsNullOrEmpty(response.Content))
            {
                dynamic data = JsonConvert.DeserializeObject(response.Content);
                string secret = data["data"]["trackList"][0]["location"];
                return Caesar(secret);
            }
            return null;
        }

        public async Task<List<SongViewModel>> SearchTrack(string keyword)
        {
            string searchUrl = $"http://api.xiami.com/web?v=2.0&app_key=1&key={keyword}" +
                            "&page=1&limit=50&_ksTS=1459930568781_153&callback=jsonp154" +
                            "&r=search/songs";
            var response = await HttpRequest.SendAsync(searchUrl);
            if (response.Content != null)
            {
                string jsonString = SplitString(response.Content, "jsonp154(", ")");
                dynamic data = JsonConvert.DeserializeObject(jsonString);
                var result = new List<SongViewModel>();
                foreach (var song in data["data"]["songs"])
                {
                    result.Add(ConvertSong(song));
                }
                return result;
            }
            return null;
        }

        public async Task<string> GetLyric(string trackId, string lyricUrl = null)
        {
            if(!string.IsNullOrEmpty(lyricUrl))
            {
                var response = await httpClient.GetStringAsync(lyricUrl);
                if(!string.IsNullOrEmpty(response))
                {
                    return reg.Replace(response, "");
                }
            }
            return null;
        }
    }
}
