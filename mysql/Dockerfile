# syntax = docker/dockerfile:experimental
FROM alpine
ARG MYSQL_ROOT_PASSWORD
ARG MYSQL_USER
ARG MYSQL_PASSWORD
ARG MYSQL_DB

RUN apk add --no-cache mariadb mariadb-client mariadb-server-utils pwgen && rm -f /var/cache/apk/*

RUN mkdir -p /docker-entrypoint-initdb.d
RUN mkdir -p /scripts/pre-exec.d 
RUN mkdir -p /scripts/pre-init.d 

ADD mysql/setup.sh /scripts/setup.sh
ADD mysql/init.sql /scripts/init.sql

RUN chmod -R 755 /scripts
RUN chmod -R 755 /etc/init.d/

EXPOSE 3306

RUN mkdir secrets
RUN echo "$MYSQL_ROOT_PASSWORD" >> /secrets/mysql_root_password

RUN echo "$MYSQL_USER" >> /secrets/mysql_user

RUN echo "$MYSQL_PASSWORD" >> /secrets/mysql_password

RUN echo "$MYSQL_DB" >> /secrets/mysql_db

RUN ./scripts/setup.sh
ENTRYPOINT ["/bin/sh"]