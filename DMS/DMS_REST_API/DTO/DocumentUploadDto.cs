// DMS_REST_API/DTO/DocumentUploadDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DMS_REST_API.DTO
{
    public class DocumentUploadDto
    {
        [Required(ErrorMessage = "Der Dokumenttitel ist erforderlich.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Der Dokumenttyp ist erforderlich.")]
        public string FileType { get; set; }

        [Required(ErrorMessage = "Eine Datei muss hochgeladen werden.")]
        public IFormFile File { get; set; }
    }
}