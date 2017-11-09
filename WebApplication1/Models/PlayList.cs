using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("PlayList")]
    public class PlayList
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int PlayListId { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        [Column(TypeName ="nvarchar(50)")]
        public string Title { get; set; }
        [MaxLength(200)]
        public string CoverImgUrl { get; set; }
        public ICollection<Track> Tracks { get; set; }
    }
}
