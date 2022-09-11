# syntax = docker/dockerfile:experimental
FROM alpine

RUN apk update

RUN apk add --no-cache bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib curl openssl
RUN apk add libgdiplus --repository https://dl-3.alpinelinux.org/alpine/edge/testing/

#RUN openssl genrsa -des3 -passout pass:x -out server.pass.key 2048
#RUN openssl rsa -passin pass:x -in server.pass.key -out server.key
#RUN rm server.pass.key
#RUN openssl req -new -key server.key -out server.csr \
#    -subj "/C=DE/ST=Niedersachsen/L=Ettenbüttel/O=SELK/OU=JuMiG NDS/CN=example.com"
#RUN openssl x509 -req -days 365 -in server.csr -signkey server.key -out server.crt

RUN rm -f /var/cache/apk/*

RUN wget https://dot.net/v1/dotnet-install.sh

RUN chmod +x dotnet-install.sh
RUN ./dotnet-install.sh -c Current --verbose 
#--runtime aspnetcore
ENV PATH $PATH:/root/.dotnet/tools
ENV PATH $PATH:/root/.dotnet
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
RUN dotnet --version

COPY backend/API /app

EXPOSE 2000

RUN dotnet dev-certs https --trust
RUN dotnet build /app

ENTRYPOINT ["dotnet", "run", "--project", "app"]