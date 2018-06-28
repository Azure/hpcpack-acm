FROM microsoft/aspnetcore-build:2.0 AS allbuild
WORKDIR /src
COPY *.sln ./
COPY Frontend/Frontend.csproj Frontend/
COPY Common/DTO/DTO.csproj Common/DTO/
COPY Common/Utilities/Utilities.csproj Common/Utilities/
COPY Services/Common/ServicesCommon.csproj Services/Common/
COPY Services/JobMonitor/JobMonitor.csproj Services/JobMonitor/
COPY Services/TaskDispatcher/TaskDispatcher.csproj Services/TaskDispatcher/
COPY Services/NodeAgent/NodeAgent.csproj Services/NodeAgent/
COPY Bootstrap/Bootstrap.csproj Bootstrap/
RUN dotnet restore
COPY . .
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/Frontend
WORKDIR /src/Bootstrap
RUN dotnet publish -c Release -o /app/Bootstrap
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
RUN ng build

FROM evancui/hpcacm:runtime AS final
WORKDIR /app
COPY --from=portalbuild /src/portal/dist /app/portal
COPY --from=allbuild /app .
RUN echo 'server { \n\
	root /app/portal; \n\
	listen 8080; \n\
}' > /etc/nginx/conf.d/hpcacm.conf
ENTRYPOINT ["dotnet", "Bootstrap/Bootstrap.dll"]
