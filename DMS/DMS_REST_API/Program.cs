using System.Reflection;
using DMS_REST_API.Mappings;
using DMS_REST_API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using DMS_REST_API.DTO;
using DMS_DAL.Repositories;
using DMS_DAL.Data; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Debug);

builder.Services.AddControllers();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DocumentDtoValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<DMS_Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DMS_Database"))
);

// Repository registrieren
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// RabbitMQPublisher als IRabbitMQPublisher registrieren
builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

// Hosted Services registrieren (optional)


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

app.UseCors("AllowWebUI");

// Optional: HTTPS-Umleitung aktivieren
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
