# Verwende das .NET SDK für Build und Runtime
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Kopiere das Projekt und installiere Abhängigkeiten
COPY ["DMS_OCRWorker/DMS_OCRWorker.csproj", "DMS_OCRWorker/"]
RUN dotnet restore "DMS_OCRWorker/DMS_OCRWorker.csproj"
COPY . .
WORKDIR "/src/DMS_OCRWorker"
RUN dotnet publish -c Release -o /app/publish

# Runtime-Image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/publish .

# Installiere Tesseract OCR
RUN apt-get update && apt-get install -y tesseract-ocr
COPY tessdata /usr/share/tesseract-ocr/4.00/tessdata

ENTRYPOINT ["dotnet", "DMS_OCRWorker.dll"]
