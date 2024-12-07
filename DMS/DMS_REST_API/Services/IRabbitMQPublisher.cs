using DMS_REST_API.DTO;

namespace DMS_REST_API.Services
{
    public interface IRabbitMQPublisher
    {
        void PublishDocumentCreated(DocumentDto document);
        void PublishDocumentUpdated(DocumentDto document);
        void PublishDocumentDeleted(int documentId);
        
        
    }
}