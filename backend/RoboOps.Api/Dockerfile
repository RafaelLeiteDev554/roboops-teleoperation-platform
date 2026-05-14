FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RoboOps.slnx ./
COPY backend/RoboOps.Api/RoboOps.Api.csproj backend/RoboOps.Api/
RUN dotnet restore backend/RoboOps.Api/RoboOps.Api.csproj

COPY backend/RoboOps.Api backend/RoboOps.Api
RUN dotnet publish backend/RoboOps.Api/RoboOps.Api.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RoboOps.Api.dll"]
