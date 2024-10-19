using System.Reflection;
using DMS_REST_API.DataPersistence; // Nimm an, ApplicationDbContext ist in diesem Namespace
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Registriere den ApplicationDbContext mit PostgreSQL (ORM)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registriere das DocumentRepository für Dependency Injection
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI",
        policy =>
        {
            policy.WithOrigins("http://localhost")
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod();
        });
});

// Swagger und API Explorer für die Dokumentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWebUI");

app.Urls.Add("http://*:8080");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
