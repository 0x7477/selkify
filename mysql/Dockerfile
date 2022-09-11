# syntax = docker/dockerfile:experimental
FROM alpine

RUN apk add --no-cache mariadb mariadb-client mariadb-server-utils pwgen && rm -f /var/cache/apk/*

RUN mkdir -p /docker-entrypoint-initdb.d
RUN mkdir -p /scripts/pre-exec.d 
RUN mkdir -p /scripts/pre-init.d 

ADD mysql/run.sh /scripts/run.sh
ADD mysql/init.sql /scripts/init.sql

RUN chmod -R 755 /scripts
RUN chmod -R 755 /etc/init.d/

EXPOSE 3306

RUN mkdir secrets
RUN --mount=type=secret,id=MYSQL_ROOT_PASSWORD \
    echo "$(cat /run/secrets/MYSQL_ROOT_PASSWORD)" >> /secrets/mysql_root_password

RUN --mount=type=secret,id=MYSQL_USER \
    echo "$(cat /run/secrets/MYSQL_USER)" >> /secrets/mysql_user

RUN --mount=type=secret,id=MYSQL_PASSWORD \
    echo "$(cat /run/secrets/MYSQL_PASSWORD)" >> /secrets/mysql_password

RUN --mount=type=secret,id=MYSQL_DB \
    echo "$(cat /run/secrets/MYSQL_DB)" >> /secrets/mysql_db

ENTRYPOINT ["/scripts/run.sh"]