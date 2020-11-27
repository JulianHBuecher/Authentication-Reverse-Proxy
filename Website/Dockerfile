FROM nginx:latest

# RUN apt-get update && apt-get upgrade -y
# RUN apt-get install php-common php-cli php-fpm -y

COPY nginx.conf /etc/nginx/conf.d/nginx.conf

RUN mkdir /usr/share/nginx/html/assets

ADD sites /usr/share/nginx/html
ADD assets /usr/share/nginx/html/assets