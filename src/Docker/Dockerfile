FROM microsoft/aspnetcore:2.0 AS python
WORKDIR /app
RUN apt update
RUN apt install python-pip -y
RUN pip install psutil
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
COPY Services/NodeAgent/NodeAgent.csproj Services/NodeAgent/
COPY Bootstrap/Bootstrap.csproj Bootstrap/
RUN dotnet restore
COPY . .
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/Frontend
WORKDIR /src/Bootstrap
RUN dotnet publish -c Release -o /app/Bootstrap
WORKDIR /src/Services/JobDispatcher
RUN dotnet publish -c Release -o /app/JobDispatcher
WORKDIR /src/Services/NodeAgent
RUN dotnet publish -c Release -o /app/NodeAgent

FROM python AS final
WORKDIR /app
COPY --from=allbuild /app .
ENTRYPOINT ["dotnet", "Bootstrap/Bootstrap.dll"]
