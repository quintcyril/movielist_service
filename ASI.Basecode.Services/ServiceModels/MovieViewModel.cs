using System;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    public class MovieViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(50)]
        public string Genre { get; set; }

        [Range(1888, 2200, ErrorMessage = "Release year is out of range.")]
        public int ReleaseYear { get; set; }

        public bool Watched { get; set; }
    }
}
