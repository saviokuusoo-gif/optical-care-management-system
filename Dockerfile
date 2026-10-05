FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["optical care management system.csproj", "./"]
RUN dotnet restore "optical care management system.csproj"

COPY . .
RUN dotnet publish "optical care management system.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} dotnet \"optical care management system.dll\""]
