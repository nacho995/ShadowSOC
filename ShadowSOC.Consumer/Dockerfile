FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ShadowSOC.Shared/ShadowSOC.Shared.csproj ShadowSOC.Shared/
COPY ShadowSOC.Consumer/ShadowSOC.Consumer.csproj ShadowSOC.Consumer/
RUN dotnet restore ShadowSOC.Consumer/ShadowSOC.Consumer.csproj
COPY ShadowSOC.Shared/ ShadowSOC.Shared/
COPY ShadowSOC.Consumer/ ShadowSOC.Consumer/
RUN dotnet publish ShadowSOC.Consumer/ShadowSOC.Consumer.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShadowSOC.Consumer.dll"]
