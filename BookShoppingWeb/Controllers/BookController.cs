using BookShoppingWeb.Models;
using BookShoppingWeb.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace BookShoppingWeb.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class BookController : Controller
    {
        private readonly IBookRepository _bookRepo;
        private readonly IGenreRepository _genreRepo;
        private readonly IFileService _fileService;

        public BookController(IBookRepository bookRepo, IGenreRepository genreRepo, IFileService fileService)
        {
            _bookRepo = bookRepo;
            _genreRepo = genreRepo;
            _fileService = fileService;
        }
        public async Task<IActionResult> Index()
        {
            var books = await _bookRepo.GetBooks();
            return View(books);
        }

        //public async Task<IActionResult> AddBook()
        //{
        //    var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        //    {
        //        Text = genre.GenreName,
        //        Value = genre.Id.ToString(),
        //    });
        //    BookDTO bookToAdd = new() { GenreList = genreSelectList };
        //    return View(bookToAdd);
        //}
        public async Task<IActionResult> AddBook()
        {
            var genres = await _genreRepo.GetGenres();

            BookDTO model = new()
            {
                GenreList = genres.Select(g => new SelectListItem
                {
                    Text = g.GenreName,
                    Value = g.Id.ToString()
                })
            };

            return View(model);
        }
        //[HttpPost]
        //public async Task<IActionResult> AddBook(BookDTO bookToAdd)
        //{
        //    var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        //    {
        //        Text = genre.GenreName,
        //        Value = genre.Id.ToString(),
        //        Selected = genre.Id == bookToAdd.GenreId
        //    });

        //    bookToAdd.GenreList = genreSelectList;

        //    if (!ModelState.IsValid)
        //        return View(bookToAdd);

        //    try
        //    {
        //        // Save image if uploaded
        //        if (bookToAdd.ImageFile != null)
        //        {
        //            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
        //            bookToAdd.Image = await _fileService.SaveFile(bookToAdd.ImageFile, allowedExtensions);
        //        }

        //        Book book = new()
        //        {
        //            BookName = bookToAdd.BookName,
        //            AuthorName = bookToAdd.AuthorName,
        //            GenreId = bookToAdd.GenreId,
        //            Price = bookToAdd.Price,
        //            Image = bookToAdd.Image
        //        };

        //        await _bookRepo.AddBook(book);

        //        TempData["successMessage"] = "Book added successfully";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception)
        //    {
        //        TempData["errorMessage"] = "Error saving book";
        //        return View(bookToAdd);
        //    }
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(BookDTO model)
        {
            // reload dropdown
            var genres = await _genreRepo.GetGenres();
            model.GenreList = genres.Select(g => new SelectListItem
            {
                Text = g.GenreName,
                Value = g.Id.ToString()
            });

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // save image
                if (model.ImageFile != null)
                {
                    string[] allowed = { ".jpg", ".jpeg", ".png" };
                    model.Image = await _fileService.SaveFile(model.ImageFile, allowed);
                }

                Book book = new()
                {
                    BookName = model.BookName,
                    AuthorName = model.AuthorName,
                    GenreId = model.GenreId,
                    Price = model.Price,
                    Description = model.Description,
                    Image = model.Image,
                };

                await _bookRepo.AddBook(book);

                TempData["successMessage"] = "Book added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["errorMessage"] = "Error saving book.";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBook(BookDTO bookToUpdate)
        {
            var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
            {
                Text = genre.GenreName,
                Value = genre.Id.ToString(),
                Selected = genre.Id == bookToUpdate.GenreId
            });
            bookToUpdate.GenreList = genreSelectList;

            if (!ModelState.IsValid)
                return View(bookToUpdate);

            try
            {
                string oldImage = "";
                if (bookToUpdate.ImageFile != null)
                {
                    if (bookToUpdate.ImageFile.Length > 1 * 1024 * 1024)
                    {
                        throw new InvalidOperationException("Image file can not exceed 1 MB");
                    }
                    string[] allowedExtensions = { ".jpeg", ".jpg", ".png" };
                    string imageName = await _fileService.SaveFile(bookToUpdate.ImageFile, allowedExtensions);
                    // hold the old image name. Because we will delete this image after updating the new
                    oldImage = bookToUpdate.Image;
                    bookToUpdate.Image = imageName;
                }
                // manual mapping of BookDTO -> Book
                Book book = new()
                {
                    Id = bookToUpdate.Id,
                    BookName = bookToUpdate.BookName,
                    AuthorName = bookToUpdate.AuthorName,
                    GenreId = bookToUpdate.GenreId,
                    Price = bookToUpdate.Price,
                    Image = bookToUpdate.Image
                };
                await _bookRepo.UpdateBook(book);
                // if image is updated, then delete it from the folder too
                if (!string.IsNullOrWhiteSpace(oldImage))
                {
                    _fileService.DeleteFile(oldImage);
                }
                TempData["successMessage"] = "Book is updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(bookToUpdate);
            }
            catch (FileNotFoundException ex)
            {
                TempData["errorMessage"] = ex.Message;
                return View(bookToUpdate);
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error on saving data";
                return View(bookToUpdate);
            }
        }
        // ===============================
        // DELETE BOOK
        // ===============================
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _bookRepo.GetBookById(id);

            if (book != null)
            {
                await _bookRepo.DeleteBook(book);

                if (!string.IsNullOrEmpty(book.Image))
                    _fileService.DeleteFile(book.Image);
            }

            return RedirectToAction(nameof(Index));
        }

        //Edit Book
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _bookRepo.GetBookById(id);
            var genres = await _genreRepo.GetGenres();
            var dto = new BookDTO
            {
                Id = book.Id,
                BookName = book.BookName,
                AuthorName = book.AuthorName,
                Price = book.Price,
                Description = book.Description,
                GenreId = book.GenreId,
                Image = book.Image,
                GenreList = genres.Select(g=> new SelectListItem
                {
                    Text = g.GenreName,
                    Value = g.Id.ToString(),
                    Selected = g.Id == book.GenreId
                })
            };
            return View(dto);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(BookDTO dto)
        {

            if (!ModelState.IsValid)
            {
                var genres = await _genreRepo.GetGenres();
                dto.GenreList = genres.Select(g => new SelectListItem
                {
                    Text = g.GenreName,
                    Value = g.Id.ToString(),
                    Selected = g.Id == dto.GenreId
                });
                return View(dto);
            }
            var book = await _bookRepo.GetBookById(dto.Id);

            book.BookName = dto.BookName;
            book.AuthorName = dto.AuthorName;
            book.Price = dto.Price;
            book.Description = dto.Description;

            if(dto.ImageFile != null)
            {
                string[] allowed = { ".jpg", ".jpeg", ".png" };
                book.Image = await _fileService.SaveFile(dto.ImageFile, allowed);
            }
            await _bookRepo.UpdateBook(book);
            TempData["successMessage"] = "Book updated Successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
