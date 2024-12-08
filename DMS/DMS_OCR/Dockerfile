FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Installieren von Ghostscript und Abhängigkeiten
RUN apt-get update && \
    apt-get install -y libgdiplus libc6-dev libx11-dev libx11-6 libxext6 libxrender-dev libxtst6 libxi6  && \
    apt-get install -y ghostscript && \
    rm -rf /var/lib/apt/lists/*


# Install required libraries
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    libleptonica-dev \
    libtesseract-dev \
    libc6-dev \
    libjpeg62-turbo-dev \
    libgdiplus \
    ghostscript \
    libpng-dev 


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DMS_OCR/DMS_OCR.csproj", "DMS_OCR/"]
RUN dotnet restore "DMS_OCR/DMS_OCR.csproj"
COPY . .
WORKDIR "/src/DMS_OCR"
RUN dotnet build "DMS_OCR.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DMS_OCR.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DMS_OCR.dll"]
