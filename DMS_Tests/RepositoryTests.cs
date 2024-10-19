using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DMS_REST_API.DataPersistence;

namespace DMS_Tests
{
    public class RepositoryTests
    {
        [Fact]
        public async Task AddAsync_ShouldAddDocument()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseInMemoryDatabase(databaseName: "TestDatabase")
                            .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var repository = new DocumentRepository(context);
                var document = new DMS_REST_API.Entities.Document { Name = "Test Document", FileType = "pdf", CreatedAt = DateTime.Now };

                await repository.AddAsync(document);

                var result = await context.Documents.FirstOrDefaultAsync(d => d.Name == "Test Document");
                Assert.NotNull(result);
                Assert.Equal("Test Document", result.Name);
            }
        }

    }
}
