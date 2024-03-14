#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["examples/DockerExample/DockerExample.csproj", "examples/DockerExample/"]
COPY ["src/Gaaaabor.Akka.Discovery.Docker/Gaaaabor.Akka.Discovery.Docker.csproj", "src/Gaaaabor.Akka.Discovery.Docker/"]

WORKDIR /src

RUN dotnet restore "examples/DockerExample/DockerExample.csproj"

COPY . .
WORKDIR "/src/examples/DockerExample"
RUN dotnet build "DockerExample.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "DockerExample.csproj" -c Debug -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerExample.dll"]