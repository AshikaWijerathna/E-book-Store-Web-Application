//using BookShoppingWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingWeb.Repositories
{
    public class HomeRepository:IHomeRepository
    {
        private readonly ApplicationDbContext _db;
        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _db.Genres.ToListAsync();
        }
        public async Task<IEnumerable<Book>>GetBooks(string sTerm="", int genreId = 0)
        {
            sTerm = sTerm?.ToLower();
            var books = from book in _db.Books
                        join genre in _db.Genres
                        on book.GenreId equals genre.Id
                        where string.IsNullOrWhiteSpace(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
                        select new Book
                        {
                            Id = book.Id,
                            Image = book.Image,
                            AuthorName = book.AuthorName,
                            BookName = book.BookName,
                            GenreId = book.GenreId,
                            Description=book.Description,
                            Price = book.Price,
                            GenreName = genre.GenreName,
                            Quantity = book.Stock == null? 0:book.Stock.Quantity
                        };
            if (!string.IsNullOrWhiteSpace(sTerm))
            {
                books = books.Where(a => a.BookName.ToLower().StartsWith(sTerm));
            }
            if (genreId > 0)
            {
                books = books.Where(a => a.GenreId == genreId);
            }
            return await books.ToListAsync();

        }
    }
}