using DMS_DAL.Data;
using DMS_DAL.Repositories;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<DMS_Context>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DMS_Database"))
);
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
var app = builder.Build();
app.Run();
