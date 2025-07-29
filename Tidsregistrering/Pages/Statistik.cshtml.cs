using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tidsregistrering.Data;
using Tidsregistrering.Models;

namespace Tidsregistrering.Pages
{
    public class StatistikModel : PageModel
    {
        private readonly TidsregistreringContext _context;

        public StatistikModel(TidsregistreringContext context)
        {
            _context = context;
        }

        // User info
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // Overall statistics
        public int TotalRegistreringer { get; set; }
        public int TotalTimer { get; set; }
        public int TotalMinutter { get; set; }
        public int TotalMinutterAlt { get; set; }
        public int GennemsnitTimer { get; set; }
        public int GennemsnitMinutter { get; set; }
        public int AntalAfdelinger { get; set; }

        // Department statistics
        public Dictionary<string, AfdelingStatModel> AfdelingStats { get; set; } = new();
        public string MestBrugteAfdeling { get; set; } = string.Empty;

        // Date info
        public Registrering? SenesteRegistrering { get; set; }
        public Registrering? FørsteRegistrering { get; set; }

        // Calculated properties
        public decimal TotalTimerDecimal => Math.Round((decimal)TotalMinutterAlt / 60, 1);
        public decimal ArbejdsdageDecimal => Math.Round((decimal)TotalMinutterAlt / 480, 2);

        public async Task OnGetAsync()
        {
            LoadUserInfo();
            await LoadStatisticsAsync();
        }

        private void LoadUserInfo()
        {
            Username = User.Identity?.Name ?? "Unknown";

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

        private async Task LoadStatisticsAsync()
        {
            var registreringer = await _context.Registreringer
                .Where(r => r.Brugernavn == Username)
                .OrderByDescending(r => r.Dato)
                .ToListAsync();

            if (!registreringer.Any())
            {
                return;
            }

            // Overall statistics
            TotalRegistreringer = registreringer.Count;
            TotalMinutterAlt = registreringer.Sum(r => r.Minutter);
            TotalTimer = TotalMinutterAlt / 60;
            TotalMinutter = TotalMinutterAlt % 60;

            // Average per registration
            var gennemsnitMinutter = (double)TotalMinutterAlt / TotalRegistreringer;
            GennemsnitTimer = (int)(gennemsnitMinutter / 60);
            GennemsnitMinutter = (int)(gennemsnitMinutter % 60);

            // Department statistics
            var afdelingGroups = registreringer.GroupBy(r => r.Afdeling);

            foreach (var group in afdelingGroups)
            {
                var afdelingMinutter = group.Sum(r => r.Minutter);
                var procent = Math.Round((double)afdelingMinutter / TotalMinutterAlt * 100, 1);

                AfdelingStats[group.Key] = new AfdelingStatModel
                {
                    Antal = group.Count(),
                    TotalMinutter = afdelingMinutter,
                    Timer = afdelingMinutter / 60,
                    Minutter = afdelingMinutter % 60,
                    Procent = procent
                };
            }

            // Sort by most time
            AfdelingStats = AfdelingStats
                .OrderByDescending(x => x.Value.TotalMinutter)
                .ToDictionary(x => x.Key, x => x.Value);

            AntalAfdelinger = AfdelingStats.Count;
            MestBrugteAfdeling = AfdelingStats.FirstOrDefault().Key ?? string.Empty;

            // Date ranges
            SenesteRegistrering = registreringer.First(); // Already ordered by date desc
            FørsteRegistrering = registreringer.Last();
        }
    }

    public class AfdelingStatModel
    {
        public int Antal { get; set; }
        public int TotalMinutter { get; set; }
        public int Timer { get; set; }
        public int Minutter { get; set; }
        public double Procent { get; set; }
    }
}