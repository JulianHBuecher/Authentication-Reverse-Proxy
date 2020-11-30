FROM openresty/openresty:alpine-fat

RUN apk update && apk upgrade
RUN luarocks install lua-resty-session
RUN luarocks install lua-resty-http
RUN luarocks install lua-resty-openidc

COPY ./conf.d/sites-available/website_auth_request.conf /etc/nginx/conf.d/website_auth_request.conf
COPY ./conf.d/sites-available/redirect.conf /etc/nginx/conf.d/redirect.conf

COPY ./reverse_proxy.certificates/cert.pem /etc/ssl/private/cert.pem
COPY ./reverse_proxy.certificates/key.pem /etc/ssl/private/key.pem

COPY ./basic.conf/nginx.conf /etc/nginx/nginx.conf
COPY ./basic.conf/common.conf /usr/local/openresty/nginx/conf/common.conf
COPY ./basic.conf/common_location.conf /usr/local/openresty/nginx/conf/common_location.conf
COPY ./basic.conf/dhparams.pem /etc/nginx/dhparams.pem
COPY ./basic.conf/ssl.conf /etc/nginx/ssl.conf