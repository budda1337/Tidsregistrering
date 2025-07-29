using Microsoft.EntityFrameworkCore;
using Tidsregistrering.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add Windows Authentication
builder.Services.AddAuthentication("Windows")
    .AddNegotiate();
builder.Services.AddAuthorization();

// Add Entity Framework - OPDATERET STI
builder.Services.AddDbContext<TidsregistreringContext>(options =>
    options.UseSqlite("Data Source=C:\\TidsregistreringData\\tidsregistrering.db"));

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TidsregistreringContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // VIGTIGT!
app.UseAuthorization();

app.MapRazorPages();

app.Run();