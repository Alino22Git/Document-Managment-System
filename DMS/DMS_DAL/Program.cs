using DMS_DAL.Data;
using DMS_DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddDbContext<DMS_Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DMS_Database"))
);
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
var app = builder.Build();
app.UseRouting();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DMS_Context>();

    try
    {
        Console.WriteLine("Versuche, eine Verbindung zur Datenbank herzustellen...");
        // Warte, bis die Datenbank bereit ist
        while (!context.Database.CanConnect())
        {
            Console.WriteLine("Datenbank ist noch nicht bereit, warte...");
            Thread.Sleep(1000); // Warte 1 Sekunde
        }
        Console.WriteLine("Verbindung zur Datenbank erfolgreich.");
        // Migrations anwenden und die Datenbank erstellen/aktualisieren
        context.Database.EnsureCreated();
        Console.WriteLine("Datenbankmigrationen erfolgreich angewendet.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fehler bei der Anwendung der Migrationen: {ex.Message}");
    }
}

app.MapControllers();

app.Run();
