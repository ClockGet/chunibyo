using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.Filters;
using WebApplication1.ViewModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication1.Api
{
    [Route("api/[controller]/[action]")]
    [ServiceFilter(typeof(ApiExceptionFilterAttribute))]
    public class PlayListController : Controller
    {
        private readonly ILogger _logger;
        public PlayListController(ILogger<ApiExceptionFilterAttribute> logger)
        {
            this._logger = logger;
        }
        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> Load(int source, int offset = 0)
        {
            var providerList = Replay.Helper.GetProviderList();
            List<PlayListInfoViewModel> result = null;
            if (source >= 0 && source < providerList.Length)
            {
                var provider = providerList[source];
                result = await provider.GetPlayListInfos(offset);
            }
            return Json(new { result = result });
        }
        [HttpGet]
        public async Task<IActionResult> List(string listId)
        {
            try
            {
                PlayListViewModel result = null;
                if (listId.StartsWith("my_"))
                {
                    return Json(result);
                }
                else
                {
                    var provider = Replay.Helper.GetProvider(listId);
                    string itemId = listId.Split("_")[1];
                    if (string.IsNullOrEmpty(itemId))
                    {
                        return NotFound();
                    }
                    result = await provider.GetPlayList(itemId);
                    return Json(new { is_mine = "0", tracks = result.Tracks, info = result.Info });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Artist(string artistId)
        {
            try
            {
                PlayListViewModel result = null;
                var provider = Replay.Helper.GetProvider(artistId);
                string itemId = artistId.Split("_")[1];
                if (string.IsNullOrEmpty(itemId))
                {
                    return NotFound();
                }
                result = await provider.GetArtist(itemId);
                return Json(new { is_mine = "0", status = "1", tracks = result.Tracks, info = result.Info });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Album(string albumId)
        {
            try
            {
                PlayListViewModel result = null;
                var provider = Replay.Helper.GetProvider(albumId);
                string itemId = albumId.Split("_")[1];
                if (string.IsNullOrEmpty(itemId))
                {
                    return NotFound();
                }
                result = await provider.GetAlbum(itemId);
                return Json(new { is_mine = "0", status = "1", tracks = result.Tracks, info = result.Info });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Search(int source, string keywords)
        {
            if (string.IsNullOrEmpty(keywords))
                return Json(new { result = "" });
            var providerList = Replay.Helper.GetProviderList();
            List<SongViewModel> trackList = null;
            try
            {
                if (source > -1 && source < providerList.Length)
                {
                    var provider = providerList[source];
                    trackList = await provider.SearchTrack(keywords);
                }
                return Json(new { result = trackList });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Lyric(string trackId, string lyricUrl=null)
        {
            try
            {
                var provider = Replay.Helper.GetProvider(trackId);
                string itemId = trackId.Split("_")[1];
                if (string.IsNullOrEmpty(itemId))
                {
                    return NotFound();
                }
                string data = await provider.GetLyric(itemId, lyricUrl);
                return Json(new { lyric = data });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
        public async Task<IActionResult> TrackFile(SongViewModel model)
        {
            try
            {
                var provider = Replay.Helper.GetProviderByName(model.Source);
                string songId = model.Id.Split("_")[1];
                if (string.IsNullOrEmpty(model.Id) || model.Id.IndexOf("_") == -1)
                    return NotFound();
                var url = await provider.GetUrlById(songId);
                if (string.IsNullOrEmpty(url))
                    return NotFound();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound();
            }
        }
    }
}
