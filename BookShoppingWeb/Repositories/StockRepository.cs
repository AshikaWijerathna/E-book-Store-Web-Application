using Microsoft.EntityFrameworkCore;

namespace BookShoppingWeb.Repositories
{
    public class StockRepository:IStockRepository
    {
        private readonly ApplicationDbContext _context;
        public StockRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Stock> GetStockByBookId(int bookId) =>
            await _context.Stocks.FirstOrDefaultAsync(s => s.BookId == bookId);

        public async Task ManageStock(StockDTO stockToManage)
        {
            var existingStock = await GetStockByBookId(stockToManage.BookId);
            if(existingStock is null)
            {
                var stock = new Stock { BookId = stockToManage.BookId, Quantity = stockToManage.Quantity };
                _context.Stocks.Add(stock);
            }
            else
            {
                existingStock.Quantity = stockToManage.Quantity;
            }
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm)
        {
            var stocks = await (from book in _context.Books
                                join stock in _context.Stocks
                                on book.Id equals stock.BookId
                                into book_stock
                                from bookStock in book_stock.DefaultIfEmpty()
                                where string.IsNullOrWhiteSpace(sTerm) ||
                                book.BookName.ToLower().Contains(sTerm.ToLower())
                                select new StockDisplayModel
                                {
                                    BookId = book.Id,
                                    BookName = book.BookName,
                                    Quantity = bookStock == null ? 0 : bookStock.Quantity
                                }).ToListAsync();
            return stocks;
        }

        public async Task<List<StockDisplayModel>> GetLowStockItems(int threshold = 5)
        {
            return await _context.Stocks
                .Include(s => s.Book)
                .Where(s => s.Quantity <= threshold)
                .Select(s => new StockDisplayModel
                {
                    BookId = s.BookId,
                    BookName = s.Book.BookName,
                    Quantity = s.Quantity
                })
                .OrderBy(s => s.Quantity).ToListAsync();
        }
    }
    public interface IStockRepository
    {
        Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "");
        Task<Stock?> GetStockByBookId(int bookId);
        Task ManageStock(StockDTO stockToManage);
        Task<List<StockDisplayModel>> GetLowStockItems(int threshold = 5);
    }
}
