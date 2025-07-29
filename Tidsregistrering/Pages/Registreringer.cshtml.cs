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
    }
}