FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY *.sln ./

COPY AnimeSearch.Site/ ./AnimeSearch.Site
COPY AnimeSearch.Api/ ./AnimeSearch.Api
COPY AnimeSearch.Services/ ./AnimeSearch.Services
COPY AnimeSearch.Data/ ./AnimeSearch.Data
COPY AnimeSearch.Core/ ./AnimeSearch.Core

RUN dotnet restore AnimeSearch.sln
RUN dotnet publish AnimeSearch.Site -c Release -o /Site --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /Site
COPY --from=build /Site ./

ENTRYPOINT ["dotnet", "AnimeSearch.Site.dll"]
