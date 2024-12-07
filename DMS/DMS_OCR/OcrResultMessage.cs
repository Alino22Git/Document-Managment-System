// DMS_OCR/OcrResultMessage.cs
namespace DMS_OCR
{
    public class OcrResultMessage
    {
        public string Id { get; set; }      // Eindeutige ID des Dokuments
        public string Content { get; set; } // OCR-Ergebnistext
    }
}