FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copiar arquivos do projeto
COPY EveUp.sln .
COPY EveUp.Api/*.csproj EveUp.Api/
COPY EveUp.Core/*.csproj EveUp.Core/
COPY EveUp.Services/*.csproj EveUp.Services/
COPY EveUp.Infrastructure/*.csproj EveUp.Infrastructure/

# Restaurar dependências
RUN dotnet restore EveUp.Api/EveUp.Api.csproj

# Copiar todo o código
COPY . .

# Compilar
RUN dotnet publish EveUp.Api/EveUp.Api.csproj -c Release -o /app

# Imagem final
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EveUp.Api.dll"]