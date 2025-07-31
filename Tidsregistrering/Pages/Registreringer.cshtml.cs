using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Pages
{
    public class RegistreringerModel : PageModel
    {
        private readonly TidsregistreringContext _context;

        public RegistreringerModel(TidsregistreringContext context)
        {
            _context = context;
        }

        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<Registrering> Registreringer { get; set; } = new();

        public async Task OnGetAsync()
        {
            LoadUserInfo();
            await LoadRegistreringerAsync();
        }

        private void LoadUserInfo()
        {
            Username = User.Identity?.Name ?? "Unknown";

            // Simple username parsing
            if (Username.Contains('\\'))
            {
                var parts = Username.Split('\\');
                FullName = parts[1];
            }
            else
            {
                FullName = Username;
            }
        }

        private async Task LoadRegistreringerAsync()
        {
            Registreringer = await _context.Registreringer
                .Where(r => r.Brugernavn == Username)
                .OrderByDescending(r => r.Dato)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            LoadUserInfo(); // Vigtigt! Vi skal have brugerinfo også ved POST

            var registrering = await _context.Registreringer
                .FirstOrDefaultAsync(r => r.Id == id && r.Brugernavn == Username);

            if (registrering != null)
            {
                _context.Registreringer.Remove(registrering);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int editId, int editMinutter, string editAfdeling, string? editBemærkninger, DateTime? editDatoUdført)
        {
            LoadUserInfo(); // Vigtigt! Vi skal have brugerinfo også ved POST

            var registrering = await _context.Registreringer
                .FirstOrDefaultAsync(r => r.Id == editId && r.Brugernavn == Username);

            if (registrering != null)
            {
                // Opdater registreringen
                registrering.Minutter = editMinutter;
                registrering.Afdeling = editAfdeling;
                registrering.Bemærkninger = editBemærkninger;
                registrering.DatoUdført = editDatoUdført;

                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}