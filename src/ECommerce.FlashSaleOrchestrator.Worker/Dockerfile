FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ECommerce.FlashSaleOrchestrator.Domain/ECommerce.FlashSaleOrchestrator.Domain.csproj", "src/ECommerce.FlashSaleOrchestrator.Domain/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Application/ECommerce.FlashSaleOrchestrator.Application.csproj", "src/ECommerce.FlashSaleOrchestrator.Application/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Infrastructure/ECommerce.FlashSaleOrchestrator.Infrastructure.csproj", "src/ECommerce.FlashSaleOrchestrator.Infrastructure/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Worker/ECommerce.FlashSaleOrchestrator.Worker.csproj", "src/ECommerce.FlashSaleOrchestrator.Worker/"]

RUN dotnet restore "src/ECommerce.FlashSaleOrchestrator.Worker/ECommerce.FlashSaleOrchestrator.Worker.csproj"

COPY src/ src/

RUN dotnet publish \
    "src/ECommerce.FlashSaleOrchestrator.Worker/ECommerce.FlashSaleOrchestrator.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8081

EXPOSE 8081

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "ECommerce.FlashSaleOrchestrator.Worker.dll"]