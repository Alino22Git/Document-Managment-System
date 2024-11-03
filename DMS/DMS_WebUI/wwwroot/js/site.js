// URL der API
const apiUrl = 'api/document';

// Funktion zum Abrufen und Anzeigen der API-Daten in der Tabelle
function fetchDocuments() {
    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            const tableBody = document.getElementById('apiTableBody');
            tableBody.innerHTML = ''; // Tabelle leeren bevor neue Daten eingefügt werden

            data.forEach(item => {
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

                // Aktionen-Zelle
                const actionsCell = document.createElement('td');

                // Bearbeiten-Schaltfläche
                const editButton = document.createElement('button');
                editButton.textContent = 'Bearbeiten';
                editButton.classList.add('btn', 'btn-sm', 'btn-warning', 'mr-1');
                editButton.onclick = () => {
                    // Prompt für den neuen Titel
                    const newTitle = prompt('Bitte geben Sie den neuen Titel ein:', item.title);

                    // Optional: Prompt für den neuen Dateityp (falls auch geändert werden soll)
                    // const newFileType = prompt('Bitte geben Sie den neuen Dateityp ein:', item.fileType);

                    if (newTitle !== null) {
                        // Wenn nur der Titel geändert wird und FileType unverändert bleibt
                        const updatedData = {
                            id: item.id,
                            title: newTitle,
                            fileType: item.fileType // Unveränderte FileType
                        };

                        /*
                        // Wenn auch der Dateityp geändert werden soll
                        if (newFileType !== null) {
                            updatedData.fileType = newFileType;
                        }
                        */

                        updateDocument(item.id, updatedData);
                    }
                }
                actionsCell.appendChild(editButton);

                // Löschen-Schaltfläche
                const deleteButton = document.createElement('button');
                deleteButton.textContent = 'Löschen';
                deleteButton.classList.add('btn', 'btn-sm', 'btn-danger');
                deleteButton.onclick = () => {
                    if (confirm(`Möchten Sie das Dokument "${item.title}" wirklich löschen?`)) {
                        deleteDocument(item.id);
                    }
                };
                actionsCell.appendChild(deleteButton);

                // Aktionen-Zelle zur Zeile hinzufügen
                row.appendChild(actionsCell);

                tableBody.appendChild(row);
            });

        })
        .catch(error => {
            console.error('Fehler beim Abrufen der API-Daten:', error);
            alert('Fehler beim Laden der Daten. Bitte versuchen Sie es später erneut.');
        });
}

// Funktion zum Hinzufügen eines neuen Dokuments
function addDocument() {
    const documentTitle = document.getElementById('documentTitle').value;
    const documentType = document.getElementById('documentType').value;

    if (documentTitle.trim() === '' || documentType.trim() === '') {
        alert('Bitte geben Sie sowohl einen Titel als auch einen Dateityp ein.');
        return;
    }

    const newDoc = {
        id: 0, // Backend wird die ID generieren
        title: documentTitle,
        fileType: documentType,
    };

    console.log("Neues Dokument:", newDoc);

    fetch(apiUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(newDoc)
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Aktualisieren Sie die Liste nach dem Hinzufügen
                document.getElementById('documentTitle').value = '';
                document.getElementById('documentType').value = ''; // Eingabefelder leeren
            } else {
                response.json().then(err => {
                    alert("Fehler: " + err.title);
                    console.error('Fehler beim Hinzufügen des Dokuments:', err);
                });
            }
        })
        .catch(error => console.error('Fehler:', error));
}

// Funktion zum Aktualisieren eines Dokuments
function updateDocument(documentId, updatedData) {
    fetch(`${apiUrl}/${documentId}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(updatedData)
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Aktualisieren Sie die Liste nach dem Aktualisieren
            } else {
                response.json().then(err => {
                    alert("Fehler: " + err.title);
                    console.error('Fehler beim Aktualisieren des Dokuments:', err);
                });
            }
        })
        .catch(error => console.error('Fehler:', error));
}

// Funktion zum Löschen eines Dokuments
function deleteDocument(documentId) {
    fetch(`${apiUrl}/${documentId}`, {
        method: 'DELETE'
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Aktualisieren Sie die Liste nach dem Löschen
            } else {
                response.json().then(err => {
                    alert("Fehler: " + err.title);
                    console.error('Fehler beim Löschen des Dokuments:', err);
                });
            }
        })
        .catch(error => console.error('Fehler:', error));
}

// Daten beim Laden der Seite abrufen
document.addEventListener('DOMContentLoaded', fetchDocuments);
