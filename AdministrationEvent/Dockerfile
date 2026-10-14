FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AdministrationEvent.csproj global.json ./
RUN dotnet restore AdministrationEvent.csproj
COPY . .
RUN dotnet publish AdministrationEvent.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
CMD ["sh", "-c", "exec dotnet AdministrationEvent.dll --urls http://0.0.0.0:${PORT:-8080}"]
