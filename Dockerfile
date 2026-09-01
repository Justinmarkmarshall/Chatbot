FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Chatbot.csproj", "./"]
RUN dotnet restore "Chatbot.csproj"

COPY . ./
RUN dotnet publish "Chatbot.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Chatbot.dll"]