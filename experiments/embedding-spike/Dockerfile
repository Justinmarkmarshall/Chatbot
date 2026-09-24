FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY EmbeddingSpike.csproj nuget.config packages.lock.json ./
RUN dotnet restore --locked-mode --configfile nuget.config
COPY Program.cs OnnxEmbedder.cs ./
RUN dotnet publish -c Release --no-restore -o /out

# Same runtime image family as Chatbot; no Python or model downloader.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /spike
COPY --from=build /out ./
COPY cases.json models.json ./
COPY fixtures/ ./fixtures/
USER $APP_UID
ENTRYPOINT ["dotnet", "EmbeddingSpike.dll"]

