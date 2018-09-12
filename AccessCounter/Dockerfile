FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["AccessCounter/AccessCounter.csproj", "AccessCounter/"]
RUN dotnet restore "AccessCounter/AccessCounter.csproj"
COPY . .
WORKDIR "/src/AccessCounter"
RUN dotnet build "AccessCounter.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AccessCounter.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AccessCounter.dll"]