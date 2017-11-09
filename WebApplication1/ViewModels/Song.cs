using System;

namespace WebApplication1.ViewModels
{
    public class SongViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string ArtistId { get; set; }
        public string Album { get; set; }
        public string AlbumId { get; set; }
        public string Source { get; set; }
        public string SourceUrl { get; set; }
        public string ImgUrl { get; set; }
        public string Url { get; set; }
        public override string ToString()
        {
            return $"id={Uri.EscapeDataString(Id)}&title={Uri.EscapeDataString(Title)}&artist={Uri.EscapeDataString(Artist)}&ArtistId={Uri.EscapeDataString(ArtistId)}&album={Uri.EscapeDataString(Album)}&albumId={Uri.EscapeDataString(AlbumId)}&source={Uri.EscapeDataString(Source)}&sourceUrl={Uri.EscapeDataString(SourceUrl)}&imgUrl={Uri.EscapeDataString(ImgUrl??"")}";
        }
        public string LyricUrl { get; set; }
    }
}
