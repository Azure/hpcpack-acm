FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY services/JobDispatcher/JobDispatcher.csproj services/JobDispatcher/
COPY common/dto/dto.csproj common/dto/
COPY services/Common/ServicesCommon.csproj services/Common/
COPY common/Utilities/Utilities.csproj common/Utilities/
RUN dotnet restore
COPY . .
WORKDIR /src/services/JobDispatcher
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "JobDispatcher.dll"]
