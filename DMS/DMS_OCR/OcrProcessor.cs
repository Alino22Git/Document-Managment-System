// DMS_OCR/OcrProcessor.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ImageMagick;
using DMS_OCR.Exceptions;
namespace DMS_OCR
{
    public static class OcrProcessor
    {
        public static string PerformOcr(string filePath)
        {
            var stringBuilder = new StringBuilder();

            try
            {
                using (var images = new MagickImageCollection(filePath)) // MagickImageCollection für mehrere Seiten
                {
                    foreach (var image in images)
                    {
                        // Erstellen eines temporären PNG-Dateipfades
                        var tempPngFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

                        // Bildvorverarbeitung
                        image.Density = new Density(350, 350); // Setze die Auflösung
                        // Optional: Weitere Bildbearbeitungen
                        //image.ColorType = ColorType.Grayscale;
                        //image.Contrast();
                        //image.Sharpen();
                        //image.Despeckle();
                        image.Format = MagickFormat.Png;

                        // Speichern des vorverarbeiteten Bildes als temporäre PNG-Datei
                        image.Write(tempPngFile);

                        // Verwenden der Tesseract CLI für die OCR-Verarbeitung
                        var psi = new ProcessStartInfo
                        {
                            FileName = "tesseract",
                            Arguments = $"{tempPngFile} stdout -l eng",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(psi))
                        {
                            if (process == null)
                            {
                                throw new InvalidOperationException("Tesseract-Prozess konnte nicht gestartet werden.");
                            }

                            string result = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                throw new InvalidOperationException($"Tesseract-Prozess endete mit dem Fehlercode {process.ExitCode}.");
                            }

                            stringBuilder.Append(result);
                        }

                       
                        File.Delete(tempPngFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler bei der OCR-Verarbeitung: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Innere Ausnahme: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stacktrace: {ex.StackTrace}");
                throw new OcrWorkerExceptions.OcrProcessingException("Fehler bei der OCR-Verarbeitung", ex);
            }

            return stringBuilder.ToString();
        }
    }
}
