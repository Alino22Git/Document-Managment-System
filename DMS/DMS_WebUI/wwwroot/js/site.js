// URL der API
const apiUrl = 'api/document';

// Daten beim Laden der Seite abrufen
document.addEventListener('DOMContentLoaded', () => {
    fetchDocuments();

    // Event-Listener für das Formular
    document.getElementById('uploadForm').onsubmit = async (e) => {
        e.preventDefault();

        const titleInput = document.getElementById('titleInput');
        const fileTypeInput = document.getElementById('fileTypeInput');
        const fileInput = document.getElementById('fileInput');

        const title = titleInput.value.trim();
        const fileType = fileTypeInput.value.trim();
        const file = fileInput.files[0];
        
        if (!title) {
            alert('Bitte geben Sie einen Titel ein!');
            return;
        }

        if (!fileType) {
            alert('Bitte geben Sie einen Dateityp ein!');
            return;
        }

        if (!file) {
            alert('Bitte wählen Sie eine Datei aus!');
            return;
        }

        const formData = new FormData();
        formData.append('Title', title);
        formData.append('FileType', fileType);
        formData.append('File', file);

        try {
            const response = await fetch(`api/document/upload`, {
                method: 'POST',
                body: formData,
            });

            if (!response.ok) {
                const errorData = await response.json();
                // Extrahieren Sie die spezifischen Fehlermeldungen
                let errorMessage = 'Fehler beim Hochladen.';
                if (errorData && errorData.errors) {
                    errorMessage = Object.values(errorData.errors).flat().join(' ');
                } else if (errorData && errorData.message) {
                    errorMessage = errorData.message;
                }
                throw new Error(errorMessage);
            }

            const result = await response.json();
            document.getElementById('uploadStatus').innerText = `Erfolgreich hochgeladen: ${result.fileName}`;
            // Formular zurücksetzen
            document.getElementById('uploadForm').reset();
            fetchDocuments();
        } catch (error) {
            console.error('Fehler beim Hochladen:', error);
            document.getElementById('uploadStatus').innerText = `Fehler beim Hochladen: ${error.message}`;
        }
    };
});

// Funktion zum Abrufen und Anzeigen der API-Daten in der Tabelle
async function fetchDocuments() {
    try {
        const response = await fetch(apiUrl);
        if (!response.ok) {
            throw new Error(`HTTP-Fehler! Status: ${response.status}`);
        }

        const data = await response.json();
        const tableBody = document.getElementById('apiTableBody');
        tableBody.innerHTML = ''; // Tabelle leeren bevor neue Daten eingefügt werden

        data.forEach((item) => {
            const row = document.createElement('tr');

            const idCell = document.createElement('td');
            idCell.textContent = item.id;
            row.appendChild(idCell);

            const titleCell = document.createElement('td');
            titleCell.textContent = item.title;
            row.appendChild(titleCell);

            const fileTypeCell = document.createElement('td');
            fileTypeCell.textContent = item.fileType;
            row.appendChild(fileTypeCell);

            const fileContentCell = document.createElement('td');
            fileContentCell.textContent = item.content;
            row.appendChild(fileContentCell);

            // Aktionen-Zelle
            const actionsCell = document.createElement('td');

            // Bearbeiten-Schaltfläche
            const editButton = document.createElement('button');
            editButton.textContent = 'Bearbeiten';
            editButton.classList.add('btn', 'btn-sm', 'btn-warning', 'mr-1');
            editButton.onclick = () => editDocument(item.id, item.title, item.fileType);
            actionsCell.appendChild(editButton);

            // Löschen-Schaltfläche
            const deleteButton = document.createElement('button');
            deleteButton.textContent = 'Löschen';
            deleteButton.classList.add('btn', 'btn-sm', 'btn-danger');
            deleteButton.onclick = () => deleteDocument(item.id);
            actionsCell.appendChild(deleteButton);

            row.appendChild(actionsCell);

            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Fehler beim Abrufen der API-Daten:', error);
        alert('Fehler beim Laden der Daten. Bitte versuchen Sie es später erneut.');
    }
}

// Funktion zum Bearbeiten eines Dokuments
function editDocument(id, title, fileType) {
    const newTitle = prompt('Bitte geben Sie den neuen Titel ein:', title);
    const newFileType = prompt('Bitte geben Sie den neuen Dateityp ein:', fileType);

    if (newTitle !== null && newFileType !== null) {
        const updatedData = {
            id: id,
            title: newTitle.trim(),
            fileType: newFileType.trim(),
        };

        updateDocument(id, updatedData);
    }
}

// Funktion zum Aktualisieren eines Dokuments
async function updateDocument(documentId, updatedData) {
    try {
        const response = await fetch(`${apiUrl}/${documentId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(updatedData),
        });

        if (response.ok) {
            fetchDocuments(); // Aktualisieren Sie die Liste nach dem Aktualisieren
        } else {
            const errorData = await response.json();
            let errorMessage = 'Fehler beim Aktualisieren.';
            if (errorData && errorData.message) {
                errorMessage = errorData.message;
            }
            alert(`Fehler: ${errorMessage}`);
        }
    } catch (error) {
        console.error('Fehler beim Aktualisieren des Dokuments:', error);
    }
}

// Funktion zum Löschen eines Dokuments
async function deleteDocument(documentId) {
    if (!confirm('Möchten Sie dieses Dokument wirklich löschen?')) return;

    try {
        const response = await fetch(`${apiUrl}/${documentId}`, {
            method: 'DELETE',
        });

        if (response.ok) {
            fetchDocuments(); // Aktualisieren Sie die Liste nach dem Löschen
        } else {
            const errorData = await response.json();
            let errorMessage = 'Fehler beim Löschen.';
            if (errorData && errorData.message) {
                errorMessage = errorData.message;
            }
            alert(`Fehler: ${errorMessage}`);
        }
    } catch (error) {
        console.error('Fehler beim Löschen des Dokuments:', error);
    }
}


async function search() {
    try {
        const searchText = document.getElementById('searchInput').value;

        const response = await fetch(`${apiUrl}/search/fuzzy`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', // Content-Type setzen
            },
            body: JSON.stringify(searchText), // Direkt einen JSON-String senden
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(`Fehler: ${errorData.message || response.statusText}`);
        }

        const searchResults = await response.json();
        const documents = [];

        // Sammle alle Dokumente aus der Datenbank
        for (const result of searchResults) {
            const documentResponse = await fetch(`${apiUrl}/${result.id}`);
            if (documentResponse.ok) {
                const document = await documentResponse.json();
                documents.push(document);
            } else {
                console.warn(`Dokument mit ID ${result.id} konnte nicht geladen werden.`);
            }
        }

        // Aktualisiere die Tabelle
        const tableBody = document.getElementById('apiTableBody');
        tableBody.innerHTML = ''; // Tabelle leeren bevor neue Daten eingefügt werden

        documents.forEach((item) => {
            const row = document.createElement('tr');

            const idCell = document.createElement('td');
            idCell.textContent = item.id;
            row.appendChild(idCell);

            const titleCell = document.createElement('td');
            titleCell.textContent = item.title;
            row.appendChild(titleCell);

            const fileTypeCell = document.createElement('td');
            fileTypeCell.textContent = item.fileType;
            row.appendChild(fileTypeCell);

            const fileContentCell = document.createElement('td');
            fileContentCell.textContent = item.content;
            row.appendChild(fileContentCell);

            // Aktionen-Zelle
            const actionsCell = document.createElement('td');

            const editButton = document.createElement('button');
            editButton.textContent = 'Bearbeiten';
            editButton.classList.add('btn', 'btn-sm', 'btn-warning', 'mr-1');
            editButton.onclick = () => editDocument(item.id, item.title, item.fileType);
            actionsCell.appendChild(editButton);

            const deleteButton = document.createElement('button');
            deleteButton.textContent = 'Löschen';
            deleteButton.classList.add('btn', 'btn-sm', 'btn-danger');
            deleteButton.onclick = () => deleteDocument(item.id);
            actionsCell.appendChild(deleteButton);

            row.appendChild(actionsCell);
            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Fehler beim Abrufen der Suchergebnisse:', error);
        alert(`Fehler beim Suchen: ${error.message}`);
    }
}






