using Microsoft.EntityFrameworkCore;
using DMS_REST_API.Entities;
namespace DMS_REST_API.DataPersistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
}
