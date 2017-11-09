using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    [Table("User")]
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int UserId { get; set; }
        [MaxLength(20)]
        public string UserName { get; set; }
        [MaxLength(32)]
        public string PassWord { get; set; }
        
        public DateTime Dt { get; set; }
        public DateTime LoginDt { get; set; }
        public ICollection<PlayList> PlayLists { get; set; }

    }
}
