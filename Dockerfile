FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Nullinside.Api.TwitchBot/Nullinside.Api.TwitchBot.csproj", "src/Nullinside.Api.TwitchBot/"]
RUN dotnet restore "src/Nullinside.Api.TwitchBot/Nullinside.Api.TwitchBot.csproj"
COPY src/ .
WORKDIR "/src/Nullinside.Api.TwitchBot"
RUN dotnet build "Nullinside.Api.TwitchBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Nullinside.Api.TwitchBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nullinside.Api.TwitchBot.dll"]
