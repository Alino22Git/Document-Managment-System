using System.ComponentModel.DataAnnotations;

namespace DMS_DAL.Entities
{
    public class Document
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        public string FileType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
