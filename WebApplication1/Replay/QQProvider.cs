using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Helpers;
using WebApplication1.ViewModels;

namespace WebApplication1.Replay
{
    public sealed class QQProvider : IMusicProvider
    {
        private static HttpClient httpClient = new HttpClient { Timeout=TimeSpan.FromSeconds(10)};
        class HttpRequest : HttpHelper<QQProvider> { }
        static QQProvider()
        {
            HttpRequest.HttpRegister(null, TimeSpan.FromSeconds(10), contentType: "application/x-www-form-urlencoded", keepAlive: true, headers: new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Encoding", "gzip,deflate,sdch"},
                {"Accept-Language", "zh-CN,zh;q=0.8,gl;q=0.6,zh-TW;q=0.4" },
                { "Host", "i.y.qq.com"},
                { "Referer", "http://y.qq.com/y/static/taoge/taoge_list.html?pgv_ref=qqmusic.y.topmenu"},
                {"User-Agent","Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.152 Safari/537.36" }
            });
        }
        #region 辅助方法
        private string SplitString(string source, string startMark, string stopMark)
        {
            var s = source.IndexOf(startMark);
            var e = source.LastIndexOf(stopMark);
            var jc = source.Substring(s + startMark.Length, e - (s + startMark.Length));
            return jc;
        }
        private Task<string> GetQQToken()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            string tokenUrl = "http://base.music.qq.com/fcgi-bin/fcg_musicexpress.fcg?"
                            + "json=3&guid=780782017&g_tk=938407465&loginUin=0&hostUin=0&"
                            + "format=jsonp&inCharset=GB2312&outCharset=GB2312&notice=0&"
                            + "platform=yqq&jsonpCallback=jsonCallback&needNewCode=0";
            httpClient.GetStreamAsync(tokenUrl).ContinueWith(async t => 
            {
                using (var sr = new StreamReader(t.Result))
                {
                    string s = await sr.ReadToEndAsync();
                    var jc=SplitString(s, "jsonCallback(", ");");
                    dynamic data = JsonConvert.DeserializeObject(jc);
                    string key = data["key"];
                    tcs.SetResult(key);
                }
            });
            return tcs.Task;
        }
        private string GetImageUrl(string qqImgId, string imgType)
        {
            if (string.IsNullOrEmpty(qqImgId))
                return null;
            string category = null;
            if (imgType == "artist")
                category = "mid_singer_300";
            else if (imgType == "album")
                category = "mid_album_300";
            else
                return null;
            return $"http://imgcache.qq.com/music/photo/{category}/{qqImgId[qqImgId.Length - 2]}/{qqImgId[qqImgId.Length - 1]}/{qqImgId}.jpg";
        }
        private SongViewModel ConvertSong(dynamic song)
        {
            SongViewModel d = new SongViewModel();
            d.Id = $"qqtrack_{song["songmid"]}";
            d.Title = song["songname"];
            d.Artist = song["singer"][0]["name"];
            d.ArtistId = $"qqartist_{song["singer"][0]["mid"]}";
            d.Album = song["albumname"];
            d.AlbumId = $"qqalbum_{song["albummid"]}";
            d.ImgUrl = GetImageUrl(Convert.ToString(song["albummid"]), "album");
            d.Source = "qq";
            d.SourceUrl = $"http://y.qq.com/#type=song&mid={song["songmid"]}&tpl=yqq_song_detail";
            d.Url = $"/api/playlist/trackfile?{d}";
            return d;
        }
        #endregion
        public string FileType { get => ".m4a"; }

        public async Task<PlayListViewModel> GetAlbum(string albumId)
        {
            string url = "http://i.y.qq.com/v8/fcg-bin/fcg_v8_album_info_cp.fcg" + 
                        $"?platform=h5page&albummid={albumId}&g_tk=938407465" + 
                        "&uin=0&format=jsonp&inCharset=utf-8&outCharset=utf-8" + 
                        "&notice=0&platform=h5&needNewCode=1&_=1459961045571" + 
                        "&jsonpCallback=asonglist1459961045566";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                var jc = SplitString(response.Content, "asonglist1459961045566(", ")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = GetImageUrl(albumId.ToString(), "album"),
                    Title = data["data"]["name"],
                    Id = $"qqalbum_{albumId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in (data["data"]["list"]??Enumerable.Empty<dynamic>()))
                {
                    result.Add(ConvertSong(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<PlayListViewModel> GetArtist(string artistId)
        {
            string url = "http://i.y.qq.com/v8/fcg-bin/fcg_v8_singer_track_cp.fcg" +
                        "?platform=h5page&order=listen&begin=0&num=50&singermid=" +
                        $"{artistId}&g_tk=938407465&uin=0&format=jsonp&" +
                        "inCharset=utf-8&outCharset=utf-8&notice=0&platform=" +
                        "h5&needNewCode=1&from=h5&_=1459960621777&" +
                        "jsonpCallback=ssonglist1459960621772";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                var jc = SplitString(response.Content, "ssonglist1459960621772(", ")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                PlayListInfoViewModel info = new PlayListInfoViewModel
                {
                    CoverImgUrl = GetImageUrl(artistId.ToString(), "artist"),
                    Title = data["data"]["singer_name"],
                    Id = $"qqartist_{artistId}"
                };
                var result = new List<SongViewModel>();
                foreach (var song in data["data"]["list"])
                {
                    result.Add(ConvertSong(song["musicData"]));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<PlayListViewModel> GetPlayList(string playListId)
        {
            string url = "http://i.y.qq.com/qzone-music/fcg-bin/fcg_ucc_getcdinfo_" +
                        "byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&jsonpCallback=" +
                        $"jsonCallback&nosign=1&disstid={playListId}&g_tk=5381&loginUin=0&hostUin=0" +
                        "&format=jsonp&inCharset=GB2312&outCharset=utf-8&notice=0" +
                        "&platform=yqq&jsonpCallback=jsonCallback&needNewCode=0";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                var jc = SplitString(response.Content, "jsonCallback(", ")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                PlayListInfoViewModel info = new PlayListInfoViewModel();
                string logoUrl = data["cdlist"][0]["logo"];
                info.CoverImgUrl = logoUrl;
                info.Title = data["cdlist"][0]["dissname"];
                info.Id = $"qqplaylist_{playListId}";
                var result = new List<SongViewModel>();
                foreach (var song in data["cdlist"][0]["songlist"])
                {
                    result.Add(ConvertSong(song));
                }
                return new PlayListViewModel { Info = info, Tracks = result };
            }
            return null;
        }

        public async Task<List<PlayListInfoViewModel>> GetPlayListInfos(int offset)
        {
            var page = offset / 50 + 1;
            string url = "http://i.y.qq.com/s.plcloud/fcgi-bin/fcg_get_diss_by_tag" +
                        $".fcg?categoryId=10000000&sortId={page}&sin=0&ein=49&" +
                        "format=jsonp&g_tk=5381&loginUin=0&hostUin=0&" +
                        "format=jsonp&inCharset=GB2312&outCharset=utf-8" +
                        "&notice=0&platform=yqq&jsonpCallback=" +
                        "MusicJsonCallback&needNewCode=0";
            var response = await HttpRequest.SendAsync(url);
            if (response.Content != null)
            {
                var result = new List<PlayListInfoViewModel>();
                var jc = SplitString(response.Content, "MusicJsonCallback(", ")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                foreach (var l in data["data"]["list"])
                {
                    PlayListInfoViewModel d = new PlayListInfoViewModel
                    {
                        CoverImgUrl = l["imgurl"],
                        Title = l["dissname"],
                        PlayCount = l["listennum"],
                        ListId = $"qqplaylist_{l["dissid"]}"
                    };
                    result.Add(d);
                }
                return result;
            }
            return null;
        }

        public async Task<string> GetUrlById(string songId)
        {
            string token = await GetQQToken();
            if (string.IsNullOrEmpty(token))
                return null;
            return $"http://dl.stream.qqmusic.qq.com/C200{songId}.m4a?vkey={token}&fromtag=0&guid=780782017";
        }

        public async Task<List<SongViewModel>> SearchTrack(string keyword)
        {
            string url = "http://i.y.qq.com/s.music/fcgi-bin/search_for_qq_cp?" + 
                        "g_tk=938407465&uin=0&format=jsonp&inCharset=utf-8" + 
                        "&outCharset=utf-8&notice=0&platform=h5&needNewCode=1" + 
                        $"&w={keyword}&zhidaqu=1&catZhida=1" + 
                        "&t=0&flag=1&ie=utf-8&sem=1&aggr=0&perpage=20&n=20&p=1" + 
                        "&remoteplace=txt.mqq.all&_=1459991037831&jsonpCallback=jsonp4";
            var response = await HttpRequest.SendAsync(url);
            if(response.Content!=null)
            {
                var jc = SplitString(response.Content, "jsonp4(", ")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                List<SongViewModel> result = new List<SongViewModel>();
                foreach(var song in data["data"]["song"]["list"])
                {
                    result.Add(ConvertSong(song));
                }
                return result;
            }
            return null;
        }

        public async Task<string> GetLyric(string trackId, string lyricUrl = null)
        {
            var targetUrl = "http://i.y.qq.com/lyric/fcgi-bin/fcg_query_lyric.fcg?" +
            "songmid=" + trackId +
            "&loginUin=0&hostUin=0&format=jsonp&inCharset=GB2312" +
            "&outCharset=utf-8&notice=0&platform=yqq&jsonpCallback=MusicJsonCallback&needNewCode=0";
            var response = await HttpRequest.SendAsync(targetUrl);
            if(response.Content!=null)
            {
                var jc = SplitString(response.Content, "MusicJsonCallback(",")");
                dynamic data = JsonConvert.DeserializeObject(jc);
                if(data["lyric"]!=null)
                {
                    var bytes=Convert.FromBase64String((string)data["lyric"]);
                    return Encoding.UTF8.GetString(bytes);
                }
            }
            return null;
        }
    }
}
