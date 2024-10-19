using DMS_REST_API.DataPersistence;
using DMS_REST_API.Entities;

public interface IDocumentRepository
{
    Task<Document> GetByIdAsync(int id);
    Task AddAsync(Document document);
}

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Document> GetByIdAsync(int id)
    {
        return await _context.Documents.FindAsync(id);
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();
    }
}
