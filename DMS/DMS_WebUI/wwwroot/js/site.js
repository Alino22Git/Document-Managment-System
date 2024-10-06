// URL der API
const apiUrl = '/api/';

// Funktion zum Abrufen und Anzeigen der API-Daten in der Tabelle
function fetchApiData() {
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

            // Erstellen einer neuen Tabellenzeile mit den API-Daten
            const row = document.createElement('tr');

            const messageCell = document.createElement('td');
            messageCell.textContent = data.message;
            row.appendChild(messageCell);

            const timestampCell = document.createElement('td');
            timestampCell.textContent = new Date(data.timestamp).toLocaleString();
            row.appendChild(timestampCell);

            const statusCell = document.createElement('td');
            statusCell.textContent = data.status;
            row.appendChild(statusCell);

            tableBody.appendChild(row);
        })
        .catch(error => {
            console.error('Fehler beim Abrufen der API-Daten:', error);
            alert('Fehler beim Laden der Daten. Bitte versuchen Sie es später erneut.');
        });
}

// Daten beim Laden der Seite abrufen
document.addEventListener('DOMContentLoaded', fetchApiData);
