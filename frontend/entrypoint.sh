#!/bin/sh
# Wait until the Angular build output (index.html) is present in the Nginx HTML directory
while [ ! -f /usr/share/nginx/html/index.html ]; do
  sleep 1
done
# Give Nginx a moment to be ready after files are in place
sleep 2
# Start Nginx in the foreground
exec nginx -g 'daemon off;'
