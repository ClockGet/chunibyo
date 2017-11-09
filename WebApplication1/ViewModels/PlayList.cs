using System.Collections.Generic;

namespace WebApplication1.ViewModels
{
    public class PlayListViewModel
    {
        public List<SongViewModel> Tracks { get; set; }
        public PlayListInfoViewModel Info { get; set; }
    }
}
