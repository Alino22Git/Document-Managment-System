using System.Reflection;
using DMS_REST_API.Mappings;
using DMS_REST_API.Controllers;
using DMS_REST_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging konfigurieren
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Debug);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddAutoMapper(typeof(MappingProfile));

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddHttpClient("DMS_DAL", client =>
{
    client.BaseAddress = new Uri("http://dms_dal:8081"); // URL des DAL Services in Docker
});
builder.Services.AddSingleton<RabbitMQPublisher>();
builder.Services.AddHostedService<OcrWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Entfernen oder kommentieren Sie dies
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();