﻿// URL der API
const apiUrl = 'api/document';

// Funktion zum Abrufen und Anzeigen der API-Daten in der Tabelle
function fetchDocuments() {
    fetch(apiUrl, {
        method: 'GET'
    })
        
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log(data);
            const tableBody = document.getElementById('Documents');
            tableBody.innerHTML = ''; // Tabelle leeren bevor neue Daten eingefügt werden
            if (Array.isArray(data)) {
                data.forEach(o => {
                    const li = o.createElement('li');
                    li.innerHTML = `
                    <span>Id: ${o.id} | Title: ${o.title}</span>
                `;
                    tableBody.appendChild(li); // Zeile zur Tabelle hinzufügen
                });
            }
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
        CreatedAt: Date.now()

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


// Daten beim Laden der Seite abrufen
document.addEventListener('DOMContentLoaded', fetchDocuments);
