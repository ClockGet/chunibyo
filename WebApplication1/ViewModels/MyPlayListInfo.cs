using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.ViewModels
{
    public class MyPlayListInfoViewModel
    {
        public string CoverImgUrl { get; set; }
        public string Title { get; set; }
        public int PlayCount { get; set; }
        public int ListId { get; set; }
    }
}
