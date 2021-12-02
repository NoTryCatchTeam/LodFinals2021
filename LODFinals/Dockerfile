#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["LODFinals/LODFinals.csproj", "LODFinals/"]
RUN dotnet restore "LODFinals/LODFinals.csproj"
COPY . .
WORKDIR "/src/LODFinals"
RUN dotnet build "LODFinals.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LODFinals.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LODFinals.dll"]