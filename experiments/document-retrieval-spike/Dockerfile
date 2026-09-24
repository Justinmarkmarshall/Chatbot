FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/document-retrieval-spike
COPY document-retrieval-spike/DocumentRetrievalSpike.csproj document-retrieval-spike/nuget.config document-retrieval-spike/packages.lock.json ./
RUN dotnet restore --locked-mode --configfile nuget.config
COPY embedding-spike/OnnxEmbedder.cs ../embedding-spike/
COPY document-retrieval-spike/*.cs ./
COPY document-retrieval-spike/Chunking/ ./Chunking/
RUN dotnet publish -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /spike
COPY --from=build /out ./
COPY document-retrieval-spike/cases.json document-retrieval-spike/search.sql document-retrieval-spike/inputs.lock.json ./
COPY document-retrieval-spike/corpus/ ./corpus/
COPY embedding-spike/models.json ./
USER $APP_UID
ENTRYPOINT ["dotnet", "DocumentRetrievalSpike.dll"]
