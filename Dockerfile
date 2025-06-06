FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Nombre correcto del ejecutable generado por tu proyecto
ENV APP_NET_CORE app1.dll

# Render usa la variable PORT para exponer el puerto
ENV ASPNETCORE_URLS=http://*:$(PORT)

CMD ["dotnet", "app1.dll"]