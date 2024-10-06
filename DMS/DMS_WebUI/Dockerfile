# Basis-Image
FROM nginx:alpine

# Kopiere die Nginx-Konfiguration
COPY nginx.conf /etc/nginx/nginx.conf

# Kopiere die statischen Dateien
COPY wwwroot /usr/share/nginx/html
EXPOSE 80

