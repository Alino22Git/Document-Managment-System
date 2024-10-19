using System.ComponentModel.DataAnnotations;
namespace DMS_REST_API.Entities;
public class Document
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public string FileType { get; set; }

    public DateTime CreatedAt { get; set; }
}
