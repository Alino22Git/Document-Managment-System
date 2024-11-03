// URL der API
const apiUrl = 'api/document';

// Funktion zum Abrufen und Anzeigen der API-Daten in der Tabelle
// URL der API

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
                    // Hier können Sie eine Bearbeitungsfunktion implementieren
                };
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


function addDocument() {
    const documentTitle = document.getElementById('documentTitle').value;
    const documentType = document.getElementById('documentType').value;

    if (documentTitle.trim() === '') {
        alert('Please enter a task name');
        return;
    }



    const newDoc = {
        Id: 0,
        Title: documentTitle,
        FileType: documentType,
    };

    fetch(apiUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(newDoc)
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Refresh the list after adding
                document.getElementById('documentTitle').value = '';
                document.getElementById('documentType').value = '';// Clear the input field
            } else {
                // Neues Handling für den Fall eines Fehlers (z.B. leeres Namensfeld)
                response.json().then(err => alert("Fehler: " + err.message));
                console.error('Fehler beim Hinzufügen der Aufgabe.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}
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
                fetchDocuments(); // Aktualisiere die Liste nach dem Update
            } else {
                console.error('Fehler beim Aktualisieren des Dokuments.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}
function deleteDocument(documentId) {
    fetch(`${apiUrl}/${documentId}`, {
        method: 'DELETE'
    })
        .then(response => {
            if (response.ok) {
                fetchDocuments(); // Aktualisiere die Liste nach dem Löschen
            } else {
                console.error('Fehler beim Löschen des Dokuments.');
            }
        })
        .catch(error => console.error('Fehler:', error));
}


// Daten beim Laden der Seite abrufen
document.addEventListener('DOMContentLoaded', fetchDocuments);
