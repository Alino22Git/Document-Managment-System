using System.Collections.Generic;
using System.Reflection.Emit;
using static DMS_DAL.Data.DMS_Context;
using Microsoft.EntityFrameworkCore;
using DMS_DAL.Entities;

namespace DMS_DAL.Data
{
    public class DMS_Context(DbContextOptions<DMS_Context> options) : DbContext(options)
    {
        
        public DbSet<Document>? Documents { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Manuelle Konfiguration der Tabelle
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");  // Setzt den Tabellennamen
                entity.HasKey(e => e.Id);  // Setzt den Primärschlüssel
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);  // Konfiguriert den "Name"-Spalten
            });
            base.OnModelCreating(modelBuilder);

        }
    }
}
