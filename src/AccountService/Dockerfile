FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/AccountService/AccountService.csproj src/AccountService/
RUN dotnet restore src/AccountService/AccountService.csproj
COPY src/AccountService/ src/AccountService/
RUN dotnet publish src/AccountService/AccountService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]
