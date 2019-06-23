FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["YouTubeNotifier.VTuberRankingCrawler/YouTubeNotifier.VTuberRankingCrawler.csproj", "YouTubeNotifier.VTuberRankingCrawler/"]
RUN dotnet restore "YouTubeNotifier.VTuberRankingCrawler/YouTubeNotifier.VTuberRankingCrawler.csproj"
COPY . .
WORKDIR "/src/YouTubeNotifier.VTuberRankingCrawler"
RUN dotnet build "YouTubeNotifier.VTuberRankingCrawler.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "YouTubeNotifier.VTuberRankingCrawler.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "YouTubeNotifier.VTuberRankingCrawler.dll"]