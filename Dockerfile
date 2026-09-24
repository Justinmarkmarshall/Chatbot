# syntax=docker/dockerfile:1
FROM python:3.12-slim AS model
COPY Processing/minilm.json /build/minilm.json
COPY build/fetch-minilm.py /build/fetch-minilm.py
RUN python /build/fetch-minilm.py /build/minilm.json /model-assets

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Chatbot.csproj", "./"]
COPY ["nuget.config", "./"]
RUN --mount=type=secret,id=nuget_credentials,required=true \
    NuGetPackageSourceCredentials_github="$(cat /run/secrets/nuget_credentials)" dotnet restore "Chatbot.csproj"

COPY . ./
RUN dotnet publish "Chatbot.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENV Documents__ModelRoot=/model-assets
RUN mkdir /keys && chown 1654:1654 /keys
VOLUME ["/keys"]

COPY --from=build /app/publish ./
COPY --from=model /model-assets /model-assets
USER 1654:1654
ENTRYPOINT ["dotnet", "Chatbot.dll"]
