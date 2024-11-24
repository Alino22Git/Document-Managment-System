using System;
using System.IO;
using System.Text;
using ImageMagick;
using Tesseract;

namespace DMS_OCR
{
    public static class OcrProcessor
    {
        public static string PerformOcr(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Datei nicht gefunden: {filePath}");
            }

            var ocrResult = new StringBuilder();

            using (var images = new MagickImageCollection())
            {
                images.Read(filePath);

                foreach (var image in images)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        image.Format = MagickFormat.Png;
                        image.Write(memoryStream);

                        memoryStream.Position = 0;

                        using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                        {
                            using (var img = Pix.LoadFromMemory(memoryStream.ToArray()))
                            {
                                using (var page = engine.Process(img))
                                {
                                    ocrResult.Append(page.GetText());
                                }
                            }
                        }
                    }
                }
            }

            return ocrResult.ToString();
        }
    }
}