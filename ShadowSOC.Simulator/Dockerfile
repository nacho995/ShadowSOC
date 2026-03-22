FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ShadowSOC.Shared/ShadowSOC.Shared.csproj ShadowSOC.Shared/
COPY ShadowSOC.Simulator/ShadowSOC.Simulator.csproj ShadowSOC.Simulator/
RUN dotnet restore ShadowSOC.Simulator/ShadowSOC.Simulator.csproj
COPY ShadowSOC.Shared/ ShadowSOC.Shared/
COPY ShadowSOC.Simulator/ ShadowSOC.Simulator/
RUN dotnet publish ShadowSOC.Simulator/ShadowSOC.Simulator.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShadowSOC.Simulator.dll"]
