using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("Track")]
    public class Track
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int TrackId { get; set; }
        
        public int PlayListId { get; set; }
        public PlayList PlayList { get; set; }
        
        [Column(TypeName = "nvarchar(50)")]
        public string Title { get; set; }
        
        [Column(TypeName = "nvarchar(50)")]
        public string Artist { get; set; }
        [MaxLength(30)]
        public string ArtistId { get; set; }
        
        [Column(TypeName = "nvarchar(50)")]
        public string Album { get; set; }
        [MaxLength(30)]
        public string AlbumId { get; set; }
        [MaxLength(20)]
        public string Source { get; set; }
        [MaxLength(200)]
        public string SourceUrl { get; set; }
        public string SourceId { get; set; }
        [MaxLength(200)]
        public string ImgUrl { get; set; }
        [MaxLength(800)]
        public string Url { get; set; }
        [MaxLength(200)]
        public string LyricUrl { get; set; }
    }
}
