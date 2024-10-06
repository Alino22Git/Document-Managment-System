# Basis-Image
FROM nginx:alpine

# Kopiere die Nginx-Konfiguration
COPY nginx.conf /etc/nginx/nginx.conf

# Kopiere die statischen Dateien
COPY index.html /usr/share/nginx/html/index.html
EXPOSE 80

