FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ShadowSOC.Shared/ShadowSOC.Shared.csproj ShadowSOC.Shared/
COPY ShadowSOC.Api/ShadowSOC.Api.csproj ShadowSOC.Api/
RUN dotnet restore ShadowSOC.Api/ShadowSOC.Api.csproj
COPY ShadowSOC.Shared/ ShadowSOC.Shared/
COPY ShadowSOC.Api/ ShadowSOC.Api/
RUN dotnet publish ShadowSOC.Api/ShadowSOC.Api.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ShadowSOC.Api.dll"]
