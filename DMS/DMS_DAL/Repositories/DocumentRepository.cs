using DMS_DAL.Data;
using DMS_DAL.Entities;
using DMS_DAL.Exceptions;
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
            try
            {
                return await _context.Documents.ToListAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new DataAccessLayerException("Fehler beim Abrufen der Dokumente");
            }
        }
        public async Task<Document> GetDocumentAsync(int id)
        {
            try
            {
                
            }catch (Exception e)
            {
                Console.WriteLine(e);
                throw new DataAccessLayerException("Fehler beim Abrufen des Dokuments");
            }
            return await _context.Documents.FindAsync(id);
        }

        public async Task AddDocumentAsync(Document item)
        {
            try
            {
                await _context.Documents.AddAsync(item);
                await _context.SaveChangesAsync();
            }catch (Exception e)
            {
                Console.WriteLine(e);
                throw new DataAccessLayerException("Fehler beim Hinzufügen des Dokuments");
            }
        }
        public async Task UpdateDocumentAsync(Document item)
        {
            try
            {
                _context.Documents.Update(item);
                await _context.SaveChangesAsync();
            }catch (Exception e)
            {
                Console.WriteLine(e);
                throw new DataAccessLayerException("Fehler beim Aktualisieren des Dokuments");
            }
            
        }
        public async Task DeleteDocumentAsync(int id)
        {
            try
            {
                var item = await _context.Documents.FindAsync(id);
                if (item != null)
                {
                    _context.Documents.Remove(item);
                    await _context.SaveChangesAsync();
                }
            }catch (Exception e)
            {
                Console.WriteLine(e);
                throw new DataAccessLayerException("Fehler beim Löschen des Dokuments");
            }
        }
    }
}
