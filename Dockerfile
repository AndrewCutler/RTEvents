FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RTEvents.API/RTEvents.API.csproj RTEvents.API/
COPY RTEvents.Core/RTEvents.Core.csproj RTEvents.Core/
COPY RTEvents.Database/RTEvents.Database.csproj RTEvents.Database/
RUN dotnet restore RTEvents.API/RTEvents.API.csproj

COPY RTEvents.API/ RTEvents.API/
COPY RTEvents.Core/ RTEvents.Core/
COPY RTEvents.Database/ RTEvents.Database/
RUN dotnet publish RTEvents.API/RTEvents.API.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RTEvents.API.dll"]
