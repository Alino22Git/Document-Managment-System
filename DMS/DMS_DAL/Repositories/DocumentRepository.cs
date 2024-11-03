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
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DMS_Context _context;

        public DocumentRepository(DMS_Context context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            return await _context.Documents.ToListAsync();
        }
        public async Task<Document> GetDocumentAsync(int id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task AddDocumentAsync(Document item)
        {
            await _context.Documents.AddAsync(item);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateDocumentAsync(Document item)
        {
            _context.Documents.Update(item);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteDocumentAsync(int id)
        {
            var item = await _context.Documents.FindAsync(id);
            if (item != null)
            {
                _context.Documents.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
