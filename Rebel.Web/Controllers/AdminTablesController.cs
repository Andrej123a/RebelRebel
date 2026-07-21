using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
using Rebel.Infrastructure.Data;

namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTablesController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTablesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var tables = await _context.PubTables
                .AsNoTracking()
                .OrderBy(table => table.Area)
                .ThenBy(table => table.Label)
                .ToListAsync(cancellationToken);

            return View(tables);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PubTable
            {
                Capacity = 4,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PubTable table,
            CancellationToken cancellationToken)
        {
            Normalize(table);

            if (!ModelState.IsValid)
            {
                return View(table);
            }

            var labelExists = await _context.PubTables
                .AnyAsync(
                    existingTable =>
                        existingTable.Label == table.Label,
                    cancellationToken);

            if (labelExists)
            {
                ModelState.AddModelError(
                    nameof(table.Label),
                    "A table with this label already exists.");

                return View(table);
            }

            table.Id = Guid.NewGuid();

            _context.PubTables.Add(table);
            await _context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(
            Guid id,
            CancellationToken cancellationToken)
        {
            var table = await _context.PubTables
                .FirstOrDefaultAsync(
                    existingTable => existingTable.Id == id,
                    cancellationToken);

            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            PubTable table,
            CancellationToken cancellationToken)
        {
            if (id != table.Id)
            {
                return NotFound();
            }

            Normalize(table);

            if (!ModelState.IsValid)
            {
                return View(table);
            }

            var labelExists = await _context.PubTables
                .AnyAsync(
                    existingTable =>
                        existingTable.Id != table.Id &&
                        existingTable.Label == table.Label,
                    cancellationToken);

            if (labelExists)
            {
                ModelState.AddModelError(
                    nameof(table.Label),
                    "A table with this label already exists.");

                return View(table);
            }

            var existingTable = await _context.PubTables
                .FirstOrDefaultAsync(
                    currentTable => currentTable.Id == id,
                    cancellationToken);

            if (existingTable == null)
            {
                return NotFound();
            }

            existingTable.Label = table.Label;
            existingTable.Area = table.Area;
            existingTable.Capacity = table.Capacity;
            existingTable.IsActive = table.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        private static void Normalize(PubTable table)
        {
            table.Label = table.Label.Trim();

            table.Area = string.IsNullOrWhiteSpace(table.Area)
                ? null
                : table.Area.Trim();
        }
    }
}
