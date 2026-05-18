FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TickAggregator.sln .
COPY src/TickAggregator.Simulator/ src/TickAggregator.Simulator/
RUN dotnet publish src/TickAggregator.Simulator/TickAggregator.Simulator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TickAggregator.Simulator.dll"]
