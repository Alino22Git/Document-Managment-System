using DMS_DAL.Data;
using DMS_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS_DAL.Repositories
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<Document>> GetAllDocumentsAsync();
        Task<Document> GetDocumentAsync(int id);
        Task AddDocumentAsync(Document item);
        Task UpdateDocumentAsync(Document item);
        Task DeleteDocumentAsync(int id);
    }
    public class DocumentRepository(DMS_Context context) : IDocumentRepository
    {
        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            return await context.Documents.ToListAsync();
        }
        public async Task<Document> GetDocumentAsync(int id)
        {
            return await context.Documents.FindAsync(id);
        }

        public async Task AddDocumentAsync(Document item)
        {
            await context.Documents.AddAsync(item);
            await context.SaveChangesAsync();
        }
        public async Task UpdateDocumentAsync(Document item)
        {
            context.Documents.Update(item);
            await context.SaveChangesAsync();
        }
        public async Task DeleteDocumentAsync(int id)
        {
            var item = await context.Documents.FindAsync(id);
            if (item != null)
            {
                context.Documents.Remove(item);
                await context.SaveChangesAsync();
            }
        }

    }
}
