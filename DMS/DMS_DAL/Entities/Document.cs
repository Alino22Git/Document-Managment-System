using System.ComponentModel.DataAnnotations;

namespace DMS_DAL.Entities
{
    public class Document
    {
        public int Id { get; set; }
      
        public string Title { get; set; }
        
        public string FileType { get; set; }

        public string? OcrText { get; set; }
    }
}
