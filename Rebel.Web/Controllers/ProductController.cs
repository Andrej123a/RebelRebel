using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
using Rebel.Infrastructure.Data;

namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // INDEX

        public async Task<IActionResult> Index(
            string? searchTerm,
            Guid? categoryId,
            bool? isAvailable,
            string? tag)
        {
            var products = await GetFilteredProducts(
                searchTerm,
                categoryId,
                isAvailable,
                tag);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsAvailable = isAvailable;
            ViewBag.Tag = tag;
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id",
                "Name",
                categoryId
            );

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> FilterProducts(
            string? searchTerm,
            Guid? categoryId,
            bool? isAvailable,
            string? tag)
        {
            var products = await GetFilteredProducts(
                searchTerm,
                categoryId,
                isAvailable,
                tag);
            return PartialView("_ProductsTablePartial", products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            product.IsAvailable = !product.IsAvailable;

            await _context.SaveChangesAsync();

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Ok(new
                {
                    product.Id,
                    product.IsAvailable
                });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<Rebel.Domain.Entities.Product>> GetFilteredProducts(
            string? searchTerm,
            Guid? categoryId,
            bool? isAvailable,
            string? tag)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{searchTerm}%") ||
                    p.Description != null &&
                    EF.Functions.ILike(p.Description, $"%{searchTerm}%"));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == isAvailable.Value);
            }

            query = tag switch
            {
                "popular" => query.Where(p => p.IsPopular),
                "spicy" => query.Where(p => p.IsSpicy),
                "vegetarian" => query.Where(p => p.IsVegetarian),
                "vegan" => query.Where(p => p.IsVegan),
                "gluten-free" => query.Where(p => p.IsGlutenFree),
                "nuts" => query.Where(p => p.ContainsNuts),
                "limited" => query.Where(p => p.IsLimited),
                "promo" => query.Where(p => p.IsPromo),
                _ => query
            };

            return await query
                .OrderBy(p => p.Name)
                .ToListAsync();
        }


        // CREATE GET
        public async Task<IActionResult> Create(Guid? categoryId)
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var product = new Product
            {
                IsAvailable = true
            };

            if (categoryId.HasValue)
            {
                product.CategoryId = categoryId.Value;
            }

            return View(product);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }

            product.Id = Guid.NewGuid();

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{product.Name} was added to the menu board.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(product);
            }

            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.IsAvailable = product.IsAvailable;
            existingProduct.IsPopular = product.IsPopular;
            existingProduct.IsSpicy = product.IsSpicy;
            existingProduct.IsVegetarian = product.IsVegetarian;
            existingProduct.IsVegan = product.IsVegan;
            existingProduct.IsGlutenFree = product.IsGlutenFree;
            existingProduct.ContainsNuts = product.ContainsNuts;
            existingProduct.IsLimited = product.IsLimited;
            existingProduct.IsPromo = product.IsPromo;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{existingProduct.Name} was updated.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            product.IsDeleted = true;
            product.DeletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{product.Name} was archived from the menu board.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ByCategory(Guid id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
    }
}
