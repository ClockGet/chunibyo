using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.ViewModels;

namespace WebApplication1.Replay
{
    public interface IMusicProvider
    {
        string FileType { get; }
        Task<string> GetUrlById(string songId);
        Task<List<PlayListInfoViewModel>> GetPlayListInfos(int offset);
        Task<PlayListViewModel> GetPlayList(string playListId);
        Task<PlayListViewModel> GetArtist(string artistId);
        Task<PlayListViewModel> GetAlbum(string albumId);
        Task<List<SongViewModel>> SearchTrack(string keyword);
        Task<string> GetLyric(string trackId, string lyricUrl=null);
    }
}
