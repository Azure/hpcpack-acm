FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 5000

FROM microsoft/aspnetcore-build:2.0 AS allbuild
WORKDIR /src
COPY *.sln ./
COPY Frontend/Frontend.csproj Frontend/
COPY Common/DTO/DTO.csproj Common/DTO/
COPY Common/Utilities/Utilities.csproj Common/Utilities/
COPY Services/Common/ServicesCommon.csproj Services/Common/
COPY Services/JobDispatcher/JobDispatcher.csproj Services/JobDispatcher/
COPY Services/JobRunner/JobRunner.csproj Services/JobRunner/
COPY Services/NodeReporter/NodeReporter.csproj Services/NodeReporter/
COPY Bootstrap/Bootstrap.csproj Bootstrap/
RUN dotnet restore
COPY . .
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/Frontend
WORKDIR /src/Bootstrap
RUN dotnet publish -c Release -o /app/Bootstrap
WORKDIR /src/Services/JobDispatcher
RUN dotnet publish -c Release -o /app/JobDispatcher
WORKDIR /src/Services/NodeReporter
RUN dotnet publish -c Release -o /app/NodeReporter
WORKDIR /src/Services/JobRunner
RUN dotnet publish -c Release -o /app/JobRunner

FROM base AS final
WORKDIR /app
COPY --from=allbuild /app .
ENTRYPOINT ["dotnet", "Bootstrap/Bootstrap.dll"]
