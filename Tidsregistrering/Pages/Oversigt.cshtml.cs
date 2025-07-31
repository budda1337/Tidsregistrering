using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Pages
{
    public class OversigtModel : PageModel
    {
        private readonly TidsregistreringContext _context;

        public OversigtModel(TidsregistreringContext context)
        {
            _context = context;
        }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public DateTime? FraDato { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? TilDato { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ValgtAfdeling { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ValgtBruger { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "Dato";

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        // Data properties
        public List<Registrering> AlleRegistreringer { get; set; } = new();
        public List<string> Afdelinger { get; set; } = new();
        public List<string> Brugere { get; set; } = new();

        // Statistics
        public int TotalAntalRegistreringer { get; set; }
        public int TotalTimer { get; set; }
        public int TotalMinutter { get; set; }
        public decimal TotalTimerDecimal { get; set; }

        public async Task OnGetAsync()
        {
            await LoadFiltersAsync();
            await LoadRegistreringerAsync();
            CalculateStatistics();
        }

        private async Task LoadFiltersAsync()
        {
            // Hent alle unikke afdelinger
            Afdelinger = await _context.Registreringer
                .Select(r => r.Afdeling)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Hent alle unikke brugere (med pæne navne)
            Brugere = await _context.Registreringer
                .Where(r => !string.IsNullOrEmpty(r.FuldeNavn))
                .Select(r => r.FuldeNavn!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        private async Task LoadRegistreringerAsync()
        {
            var query = _context.Registreringer.AsQueryable();

            // Apply filters
            if (FraDato.HasValue)
            {
                query = query.Where(r => r.Dato.Date >= FraDato.Value.Date);
            }

            if (TilDato.HasValue)
            {
                query = query.Where(r => r.Dato.Date <= TilDato.Value.Date);
            }

            if (!string.IsNullOrEmpty(ValgtAfdeling))
            {
                query = query.Where(r => r.Afdeling == ValgtAfdeling);
            }

            if (!string.IsNullOrEmpty(ValgtBruger))
            {
                query = query.Where(r => r.FuldeNavn == ValgtBruger);
            }

            // Apply sorting
            query = SortBy switch
            {
                "Bruger" => SortDescending
                    ? query.OrderByDescending(r => r.FuldeNavn)
                    : query.OrderBy(r => r.FuldeNavn),
                "Afdeling" => SortDescending
                    ? query.OrderByDescending(r => r.Afdeling)
                    : query.OrderBy(r => r.Afdeling),
                "Tid" => SortDescending
                    ? query.OrderByDescending(r => r.Minutter)
                    : query.OrderBy(r => r.Minutter),
                _ => SortDescending
                    ? query.OrderByDescending(r => r.Dato)
                    : query.OrderBy(r => r.Dato)
            };

            AlleRegistreringer = await query.ToListAsync();
        }

        private void CalculateStatistics()
        {
            TotalAntalRegistreringer = AlleRegistreringer.Count;
            var totalMinutter = AlleRegistreringer.Sum(r => r.Minutter);
            TotalTimer = totalMinutter / 60;
            TotalMinutter = totalMinutter % 60;
            TotalTimerDecimal = Math.Round((decimal)totalMinutter / 60, 1);
        }

        public string GetSortIcon(string column)
        {
            if (SortBy != column) return "bi-arrow-down-up";
            return SortDescending ? "bi-sort-down" : "bi-sort-up";
        }

        public string GetSortUrl(string column)
        {
            var newSortDescending = (SortBy == column) ? !SortDescending : true;
            return $"?SortBy={column}&SortDescending={newSortDescending}" +
                   (FraDato.HasValue ? $"&FraDato={FraDato:yyyy-MM-dd}" : "") +
                   (TilDato.HasValue ? $"&TilDato={TilDato:yyyy-MM-dd}" : "") +
                   (!string.IsNullOrEmpty(ValgtAfdeling) ? $"&ValgtAfdeling={ValgtAfdeling}" : "") +
                   (!string.IsNullOrEmpty(ValgtBruger) ? $"&ValgtBruger={ValgtBruger}" : "");
        }
    }
}