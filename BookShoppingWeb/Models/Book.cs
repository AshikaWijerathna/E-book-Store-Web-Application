    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace BookShoppingWeb.Models
    {
        [Table("Book")]
        public class Book
        {
            public int Id { get; set; }
            [Required]
            [MaxLength(40)]
            public string? BookName { get; set; }
            [Required]
            [MaxLength(40)]
            public string? AuthorName { get; set; }
            [Required]
            [Column(TypeName = "decimal(10,2)")]
            public double Price { get; set; }
            [MaxLength(5000)]
            public string? Description { get; set; }
            public string? Image { get; set; }
            [Required]
            public int GenreId { get; set; }
            public Genre Genre { get; set; }
            public List<OrderDetail> OrderDetail { get; set; }
            public List<CartDetail> CartDetail { get; set; }
            public Stock Stock { get; set; }
            [NotMapped]
            public string GenreName { get; set; }
            [NotMapped]
            public int Quantity { get; set; }

       
        }
    }
