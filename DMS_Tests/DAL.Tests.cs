using System.Threading.Tasks;
using Xunit;
using DMS_DAL.Entities;
using DMS_DAL.Data;
using DMS_DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DAL.Tests
{
    public class DocumentRepositoryTests
    {
        private DMS_Context GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DMS_Context>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Einzigartiger Name pro Test
                .Options;
            var context = new DMS_Context(options);
            return context;
        }


        [Fact]
        public async Task AddDocumentAsync_ShouldAddDocument()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new DocumentRepository(context);

            var document = new Document { Title = "Test Document", FileType = "pdf", Content = "Test", FileName = "Test" };

            // Act
            await repository.AddDocumentAsync(document);

            // Assert
            var documents = await repository.GetAllDocumentsAsync();
            Assert.Single(documents);
            Assert.Equal("Test Document", documents.First().Title);
        }

        [Fact]
        public async Task GetDocumentAsync_ShouldReturnDocument()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new DocumentRepository(context);

            var document = new Document { Title = "Test Document", FileType = "pdf", Content = "Test", FileName = "Test" };
            await repository.AddDocumentAsync(document);

            // Act
            var result = await repository.GetDocumentAsync(document.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(document.Title, result.Title);
        }

        [Fact]
        public async Task UpdateDocumentAsync_ShouldUpdateDocument()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new DocumentRepository(context);
            var document = new Document { Title = "Test Document", FileType = "pdf", Content = "Test", FileName = "Test" };
            await repository.AddDocumentAsync(document);

            // Act
            document.Title = "New Title";
            await repository.UpdateDocumentAsync(document);

            // Assert
            var updated = await repository.GetDocumentAsync(document.Id);
            Assert.Equal("New Title", updated.Title);
        }

        [Fact]
        public async Task DeleteDocumentAsync_ShouldRemoveDocument()
        {
            // Arrange
            var context = GetInMemoryContext();
            var repository = new DocumentRepository(context);
            var document = new Document { Title = "Test Document", FileType = "pdf", Content = "Test", FileName = "Test" };
            await repository.AddDocumentAsync(document);

            // Act
            await repository.DeleteDocumentAsync(document.Id);

            // Assert
            var allDocs = await repository.GetAllDocumentsAsync();
            Assert.Empty(allDocs);
        }
    }
}