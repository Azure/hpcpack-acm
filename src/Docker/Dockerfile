FROM microsoft/aspnetcore-build:2.0 AS allbuild
WORKDIR /src
COPY *.sln ./
COPY Frontend/Frontend.csproj Frontend/
COPY Common/DTO/DTO.csproj Common/DTO/
COPY Common/Utilities/Utilities.csproj Common/Utilities/
COPY Services/Common/ServicesCommon.csproj Services/Common/
COPY Services/JobMonitor/JobMonitor.csproj Services/JobMonitor/
COPY Services/Dashboard/Dashboard.csproj Services/Dashboard/
COPY Services/TaskDispatcher/TaskDispatcher.csproj Services/TaskDispatcher/
COPY Services/NodeAgent/NodeAgent.csproj Services/NodeAgent/
COPY Bootstrap/Bootstrap.csproj Bootstrap/
RUN dotnet restore
COPY . .
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/Frontend
WORKDIR /src/Bootstrap
RUN dotnet publish -c Release -o /app/Bootstrap
WORKDIR /src/Services/Dashboard
RUN dotnet publish -c Release -o /app/Dashboard
WORKDIR /src/Services/JobMonitor
RUN dotnet publish -c Release -o /app/JobMonitor
WORKDIR /src/Services/TaskDispatcher
RUN dotnet publish -c Release -o /app/TaskDispatcher
WORKDIR /src/Services/NodeAgent
RUN dotnet publish -c Release -o /app/NodeAgent

FROM evancui/angularbuild AS portalbuild
WORKDIR /src
COPY portal/ portal/
WORKDIR /src/portal
RUN npm install
RUN ng build --prod

FROM evancui/hpcacm:runtime AS final
WORKDIR /app/scripts
COPY Docker/scripts/* ./
WORKDIR /app
COPY --from=allbuild /app .
COPY --from=portalbuild /src/portal/dist /app/Frontend/wwwroot
RUN echo "\ndaemon off;" >> /etc/nginx/nginx.conf
ENTRYPOINT ["dotnet", "Bootstrap/Bootstrap.dll"]
