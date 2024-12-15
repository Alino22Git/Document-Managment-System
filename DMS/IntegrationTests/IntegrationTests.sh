#!/usr/bin/env bash

# Basis-URL der REST-API
BASE_URL="http://localhost:8080"  # Anpassen an Ihre Umgebung

echo "Starte Integrationstests für DocumentController..."

# 1. Alle Dokumente abrufen (GET /document)
echo "TEST: GET /document"
curl -s -o /dev/null -w "%{http_code}\n" "${BASE_URL}/document"

# 2. Neues Dokument erstellen (POST /document)
echo "TEST: POST /document"
CREATE_RESPONSE=$(curl -s -X POST "${BASE_URL}/document" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Test Document","fileType":"pdf","fileName":"test.pdf","content":"test content"}')

echo "Response: $CREATE_RESPONSE"

# ID extrahieren
NEW_ID=$(echo "$CREATE_RESPONSE" | jq -r '.id')

if [ -z "$NEW_ID" ] || [ "$NEW_ID" = "null" ]; then
  echo "Konnte keine ID aus der Create-Antwort extrahieren. Bitte Skript anpassen."
  exit 1
fi

# 3. Dokument per ID abrufen (GET /document/{NEW_ID})
echo "TEST: GET /document/${NEW_ID}"
curl -s -o /dev/null -w "%{http_code}\n" "${BASE_URL}/document/${NEW_ID}"

echo "Warte 3 Sekunden..."

sleep 3
# 4. Fuzzy-Suche durchführen (POST /document/search/fuzzy)
echo "TEST: POST /document/search/fuzzy"
curl -s -o /dev/null -w "%{http_code}\n" -X POST "${BASE_URL}/document/search/fuzzy" \
  -H 'Content-Type: application/json' \
  -d '"test"'

# 5. Datei hochladen (POST /document/upload)
TEST_FILE_PATH="./test.pdf" # Pfad zu einer existierenden PDF
if [ ! -f "$TEST_FILE_PATH" ]; then
    echo "Test-Datei '${TEST_FILE_PATH}' existiert nicht. Bitte anpassen!"
    exit 1
fi

echo "TEST: POST /document/upload (multipart/form-data)"
UPLOAD_RESPONSE=$(curl -s -X POST "${BASE_URL}/document/upload" \
    -F "Title=UploadedDoc" \
    -F "FileType=pdf" \
    -F "File=@${TEST_FILE_PATH}")

echo "Upload Response: $UPLOAD_RESPONSE"

UPLOAD_ID=$(echo "$UPLOAD_RESPONSE" | jq -r '.id')
if [ -z "$UPLOAD_ID" ] || [ "$UPLOAD_ID" = "null" ]; then
  echo "Konnte keine ID aus der Upload-Antwort extrahieren. Bitte Skript anpassen."
  exit 1
fi

# 6. 3 Sekunden warten, um die OCR-Verarbeitung zu ermöglichen
echo "Warte 3 Sekunden..."
sleep 3

# Download-Test: Speichere die heruntergeladene Datei in 'downloaded.pdf'
echo "TEST: GET /document/download/${UPLOAD_ID} (Download)"
curl -s -o downloaded.pdf -w "%{http_code}\n" "${BASE_URL}/document/download/${UPLOAD_ID}"

# 7. Dokument löschen (DELETE /document/{UPLOAD_ID})
echo "TEST: DELETE /document/${UPLOAD_ID}"
curl -s -o /dev/null -w "%{http_code}\n" -X DELETE "${BASE_URL}/document/${UPLOAD_ID}"

echo "Integrationstests abgeschlossen."
