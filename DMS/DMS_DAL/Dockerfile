FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src


COPY ["./DMS_DAL/DMS_DAL.csproj", "./DMS_DAL/"]

RUN dotnet restore "./DMS_DAL/DMS_DAL.csproj"


COPY . .

WORKDIR "/src"

RUN dotnet build "./DMS_DAL/DMS_DAL.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DMS_DAL/DMS_DAL.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DMS_DAL.dll"]