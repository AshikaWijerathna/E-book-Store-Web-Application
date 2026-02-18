using System.ComponentModel.DataAnnotations;

namespace BookShoppingWeb.Models.DTOs
{
    public class GenreDTO
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(40)]
        public string GenreName { get; set; }
    }
}
