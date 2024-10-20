using System.Collections.Generic;
using System.Reflection.Emit;
using static DMS_DAL.Data.DMS_Context;
using Microsoft.EntityFrameworkCore;
using DMS_DAL.Entities;

namespace DMS_DAL.Data
{
    public class DMS_Context : DbContext
    {
        public DMS_Context(DbContextOptions<DMS_Context> options): base(options) { }

        public DbSet<Document>? Documents { get; set; }
        
    }
}
