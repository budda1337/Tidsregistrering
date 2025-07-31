using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Pages
{
    public class AdminModel : PageModel
    {
        private readonly TidsregistreringContext _context;

        public AdminModel(TidsregistreringContext context)
        {
            _context = context;
        }

        // Properties til visning
        public string CurrentUser { get; set; } = string.Empty;
        public List<MasterAfdeling> Afdelinger { get; set; } = new();
        public List<AfdelingUsageModel> AfdelingUsage { get; set; } = new();

        // Form properties
        [BindProperty]
        public AddAfdelingModel NewAfdeling { get; set; } = new();

        [BindProperty]
        public EditAfdelingModel EditAfdeling { get; set; } = new();

        public async Task OnGetAsync()
        {
            LoadUserInfo();
            await LoadAfdelingerAsync();
            await LoadAfdelingUsageAsync();
        }

        // Tilføj ny afdeling
        public async Task<IActionResult> OnPostAddAsync()
        {
            LoadUserInfo();

            if (!ModelState.IsValid)
            {
                await LoadAfdelingerAsync();
                await LoadAfdelingUsageAsync();
                return Page();
            }

            // Check om navnet allerede eksisterer
            var exists = await _context.MasterAfdelinger
                .AnyAsync(a => a.Navn.ToLower() == NewAfdeling.Navn.ToLower());

            if (exists)
            {
                ModelState.AddModelError("NewAfdeling.Navn", "En afdeling med dette navn eksisterer allerede.");
                await LoadAfdelingerAsync();
                await LoadAfdelingUsageAsync();
                return Page();
            }

            var afdeling = new MasterAfdeling
            {
                Navn = NewAfdeling.Navn.Trim(),
                Aktiv = true,
                Oprettet = DateTime.Now,
                OprettetAf = CurrentUser
            };

            _context.MasterAfdelinger.Add(afdeling);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Afdelingen '{NewAfdeling.Navn}' blev tilføjet succesfuldt!";
            NewAfdeling = new AddAfdelingModel();

            return RedirectToPage();
        }

        // Rediger afdeling navn
        public async Task<IActionResult> OnPostEditAsync()
        {
            LoadUserInfo();

            if (!ModelState.IsValid)
            {
                await LoadAfdelingerAsync();
                await LoadAfdelingUsageAsync();
                return Page();
            }

            var afdeling = await _context.MasterAfdelinger.FindAsync(EditAfdeling.Id);
            if (afdeling == null)
            {
                TempData["ErrorMessage"] = "Afdelingen blev ikke fundet.";
                return RedirectToPage();
            }

            var oldName = afdeling.Navn;

            // Check om det nye navn allerede eksisterer (undtagen denne afdeling)
            var exists = await _context.MasterAfdelinger
                .AnyAsync(a => a.Navn.ToLower() == EditAfdeling.NytNavn.ToLower() && a.Id != EditAfdeling.Id);

            if (exists)
            {
                TempData["ErrorMessage"] = "En afdeling med dette navn eksisterer allerede.";
                return RedirectToPage();
            }

            // Opdater alle eksisterende registreringer med det nye navn
            var registreringer = await _context.Registreringer
                .Where(r => r.Afdeling == oldName)
                .ToListAsync();

            foreach (var reg in registreringer)
            {
                reg.Afdeling = EditAfdeling.NytNavn.Trim();
            }

            // Opdater master afdeling
            afdeling.Navn = EditAfdeling.NytNavn.Trim();
            afdeling.Opdateret = DateTime.Now;
            afdeling.OpdateretAf = CurrentUser;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Afdelingen '{oldName}' blev ændret til '{EditAfdeling.NytNavn}' og {registreringer.Count} registreringer blev opdateret!";

            return RedirectToPage();
        }

        // Deaktiver afdeling
        public async Task<IActionResult> OnPostDeactivateAsync(int id)
        {
            LoadUserInfo();

            var afdeling = await _context.MasterAfdelinger.FindAsync(id);
            if (afdeling == null)
            {
                TempData["ErrorMessage"] = "Afdelingen blev ikke fundet.";
                return RedirectToPage();
            }

            afdeling.Aktiv = false;
            afdeling.Opdateret = DateTime.Now;
            afdeling.OpdateretAf = CurrentUser;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Afdelingen '{afdeling.Navn}' blev deaktiveret. Den vil ikke længere være tilgængelig i dropdown menuer.";

            return RedirectToPage();
        }

        // Aktiver afdeling
        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            LoadUserInfo();

            var afdeling = await _context.MasterAfdelinger.FindAsync(id);
            if (afdeling == null)
            {
                TempData["ErrorMessage"] = "Afdelingen blev ikke fundet.";
                return RedirectToPage();
            }

            afdeling.Aktiv = true;
            afdeling.Opdateret = DateTime.Now;
            afdeling.OpdateretAf = CurrentUser;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Afdelingen '{afdeling.Navn}' blev aktiveret igen.";

            return RedirectToPage();
        }

        private void LoadUserInfo()
        {
            CurrentUser = User.Identity?.Name ?? "Unknown";
        }

        private async Task LoadAfdelingerAsync()
        {
            Afdelinger = await _context.MasterAfdelinger
                .OrderBy(a => a.Aktiv ? 0 : 1) // Aktive først
                .ThenBy(a => a.Navn)
                .ToListAsync();
        }

        private async Task LoadAfdelingUsageAsync()
        {
            // Hent statistik for hvor meget hver afdeling bruges
            var usage = await _context.Registreringer
                .GroupBy(r => r.Afdeling)
                .Select(g => new AfdelingUsageModel
                {
                    AfdelingNavn = g.Key,
                    AntalRegistreringer = g.Count(),
                    TotalMinutter = g.Sum(r => r.Minutter),
                    SenesteRegistrering = g.Max(r => r.Dato)
                })
                .OrderByDescending(u => u.AntalRegistreringer)
                .ToListAsync();

            AfdelingUsage = usage;
        }
    }

    public class AddAfdelingModel
    {
        [Required(ErrorMessage = "Afdelingsnavn er påkrævet")]
        [StringLength(100, ErrorMessage = "Afdelingsnavn må maksimalt være 100 tegn")]
        public string Navn { get; set; } = string.Empty;
    }

    public class EditAfdelingModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nyt afdelingsnavn er påkrævet")]
        [StringLength(100, ErrorMessage = "Afdelingsnavn må maksimalt være 100 tegn")]
        public string NytNavn { get; set; } = string.Empty;
    }

    public class AfdelingUsageModel
    {
        public string AfdelingNavn { get; set; } = string.Empty;
        public int AntalRegistreringer { get; set; }
        public int TotalMinutter { get; set; }
        public DateTime SenesteRegistrering { get; set; }

        public int Timer => TotalMinutter / 60;
        public int Minutter => TotalMinutter % 60;
    }
}