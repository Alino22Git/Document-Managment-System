namespace DMS_REST_API.Entities
{
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

