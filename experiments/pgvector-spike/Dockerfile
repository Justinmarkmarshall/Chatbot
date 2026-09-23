FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/pgvector-spike
COPY pgvector-spike/PgvectorSpike.csproj pgvector-spike/nuget.config pgvector-spike/packages.lock.json ./
RUN dotnet restore --locked-mode --configfile nuget.config
COPY embedding-spike/OnnxEmbedder.cs ../embedding-spike/
COPY pgvector-spike/Program.cs ./
RUN dotnet publish -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /spike
COPY --from=build /out ./
COPY pgvector-spike/cases.json pgvector-spike/search.sql ./
COPY embedding-spike/models.json ./
USER $APP_UID
ENTRYPOINT ["dotnet", "PgvectorSpike.dll"]
