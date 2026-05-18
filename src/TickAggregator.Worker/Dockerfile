FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TickAggregator.sln .
COPY src/ src/
RUN dotnet publish src/TickAggregator.Worker/TickAggregator.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TickAggregator.Worker.dll"]
