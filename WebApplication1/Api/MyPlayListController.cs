using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DAL;
using WebApplication1.Filters;
using WebApplication1.Helpers;
using WebApplication1.Models;
using WebApplication1.ViewModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication1.Api
{
    [Route("api/[controller]/[action]")]
    [ServiceFilter(typeof(LoginFilter))]
    [ServiceFilter(typeof(ApiExceptionFilterAttribute))]
    public class MyPlayListController : Controller
    {
        private MySqlContext dbContext;
        public MyPlayListController(MySqlContext context)
        {
            dbContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> Show()
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var user = await dbContext.UserDb.Include(m => m.PlayLists).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id);
            if (user != null)
            {
                List<MyPlayListInfoViewModel> result = new List<MyPlayListInfoViewModel>();
                foreach (var p in user.PlayLists)
                {
                    result.Add(new MyPlayListInfoViewModel { CoverImgUrl = p.CoverImgUrl, ListId = p.PlayListId, Title = p.Title, PlayCount = 0 });
                }
                return Json(new { result = result });
            }
            return NotFound();
        }
        [HttpGet]
        public async Task<IActionResult> List(int listId)
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var playList = await dbContext.PlayListDb.Include(m => m.Tracks).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id && p.PlayListId == listId);
            if (playList != null)
            {
                var tracks = new List<SongViewModel>();
                foreach (var t in playList.Tracks)
                {
                    tracks.Add(new SongViewModel
                    {
                        Album = t.Album,
                        AlbumId = t.AlbumId,
                        Artist = t.Artist,
                        ArtistId = t.ArtistId,
                        Id = t.SourceId,
                        ImgUrl = t.ImgUrl,
                        Source = t.Source,
                        SourceUrl = t.SourceUrl,
                        Title = t.Title,
                        Url = t.Url
                    });
                }
                return new JsonResult(new { status = 1, tracks = tracks, info = new { CoverImgUrl = playList.CoverImgUrl, Title = playList.Title, Id = playList.PlayListId }, is_mine = "1" });
            }
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Create(MyPlayListAndSongViewModel model)
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var user = await dbContext.UserDb.Include(m => m.PlayLists).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id);
            if (user != null)
            {
                PlayList playList = new PlayList();
                playList.UserId = user.UserId;
                playList.Title = model.listTitle;
                playList.CoverImgUrl = @"/images/mycover.jpg";
                Track track = new Track();
                track.Album = model.album;
                track.AlbumId = model.albumId;
                track.Artist = model.artist;
                track.ArtistId = model.artistId;
                track.ImgUrl = model.imgUrl;
                track.Source = model.source;
                track.SourceId = model.id;
                track.SourceUrl = model.sourceUrl;
                track.Title = model.title;
                track.Url = model.url;
                track.LyricUrl = model.lyricUrl;
                playList.Tracks = new List<Track>();
                playList.Tracks.Add(track);
                user.PlayLists.Add(playList);
                await dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "请稍后再试" });
        }
        [HttpPost]
        public async Task<IActionResult> Add(AddSongToPlayListViewModel model)
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var playList = await dbContext.PlayListDb.Include(m => m.Tracks).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id && p.PlayListId == model.listId);
            if(playList!=null)
            {
                var track = playList.Tracks.FirstOrDefault(p => p.SourceId == model.id);
                if(track!=null)
                    return new JsonResult(new { success = false, message = "该播放列表下已存在此首歌曲" });
                track = new Track();
                track.Album = model.album;
                track.AlbumId = model.albumId;
                track.Artist = model.artist;
                track.ArtistId = model.artistId;
                track.ImgUrl = model.imgUrl;
                track.Source = model.source;
                track.SourceId = model.id;
                track.SourceUrl = model.sourceUrl;
                track.Title = model.title;
                track.Url = model.url;
                track.LyricUrl = model.lyricUrl;
                playList.Tracks.Add(track);
                await dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "没有找到对应的播放列表" });
        }
        [HttpPost]
        public async Task<IActionResult> RemoveTrack(RemoveTrackViewModel model)
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var playList = await dbContext.PlayListDb.Include(m => m.Tracks).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id && p.PlayListId == model.listId);
            if (playList != null)
            {
                var track=playList.Tracks.FirstOrDefault(p => p.SourceId == model.trackId);
                if(track==null)
                    return new JsonResult(new { success = false,message="此首歌曲已从该播放列表中删除" });
                dbContext.TrackDb.Remove(track);
                await dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "没有找到对应的播放列表" });
        }
        [HttpPost]
        public async Task<IActionResult> Remove(int listId)
        {
            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var playList = await dbContext.PlayListDb.FirstOrDefaultAsync(p => p.UserId == userViewModel.Id && p.PlayListId == listId);
            if (playList != null)
            {
                dbContext.PlayListDb.Remove(playList);
                await dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "没有找到对应的播放列表" });
        }
        [HttpPost]
        public async Task<IActionResult> Clone(string listId)
        {
            var provider = Replay.Helper.GetProvider(listId);
            if(provider==null)
                return new JsonResult(new { success = false, message = "没有找到对应的播放源" });
            string s = listId.Substring(2);
            List<SongViewModel> tracks = null;
            PlayListInfoViewModel info = null;
            if (s.StartsWith("album"))
            {
                string albumId = s.Split('_')[1];
                var album = await provider.GetAlbum(albumId);
                if(album==null)
                    return new JsonResult(new { success = false, message = "请稍后再试" });
                tracks = album.Tracks;
                info = album.Info;
            }
            else if (s.StartsWith("artist"))
            {
                string artistId = s.Split('_')[1];
                var artist = await provider.GetArtist(artistId);
                if(artist==null)
                    return new JsonResult(new { success = false, message = "请稍后再试" });
                tracks = artist.Tracks;
                info = artist.Info;
            }
            else if (s.StartsWith("playlist"))
            {
                string playlistId = s.Split('_')[1];
                var playlist = await provider.GetPlayList(playlistId);
                if(playlist==null)
                    return new JsonResult(new { success = false, message = "请稍后再试" });
                tracks = playlist.Tracks;
                info = playlist.Info;
            }
            if (tracks == null || tracks.Count == 0 || info == null)
            {
                return new JsonResult(new { success = false, message = "请稍后再试" });
            }

            var userViewModel = HttpContext.Session.Get<UserViewModel>("User");
            var user = await dbContext.UserDb.Include(m => m.PlayLists).FirstOrDefaultAsync(p => p.UserId == userViewModel.Id);
            if (user != null)
            {
                PlayList playList = new PlayList();
                playList.UserId = user.UserId;
                playList.Title = info.Title;
                playList.CoverImgUrl = info.CoverImgUrl;
                
                playList.Tracks = new List<Track>();
                foreach(var t in tracks)
                {
                    Track track = new Track();
                    track.Album = t.Album;
                    track.AlbumId = t.AlbumId;
                    track.Artist = t.Artist;
                    track.ArtistId = t.ArtistId;
                    track.ImgUrl = t.ImgUrl;
                    track.Source = t.Source;
                    track.SourceId = t.Id;
                    track.SourceUrl = t.SourceUrl;
                    track.Title = t.Title;
                    track.Url = t.Url;
                    track.LyricUrl = t.LyricUrl;
                    playList.Tracks.Add(track);
                }
                user.PlayLists.Add(playList);
                await dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "请稍后再试" });
        }
    }
}
