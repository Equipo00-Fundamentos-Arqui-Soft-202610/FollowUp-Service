FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5267

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MediTrack.FollowUpService/MediTrack.FollowUpService.API/MediTrack.FollowUpService.API.csproj", "MediTrack.FollowUpService/MediTrack.FollowUpService.API/"]
RUN dotnet restore "MediTrack.FollowUpService/MediTrack.FollowUpService.API/MediTrack.FollowUpService.API.csproj"
COPY . .
WORKDIR "/src/MediTrack.FollowUpService/MediTrack.FollowUpService.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MediTrack.FollowUpService.API.dll"]
