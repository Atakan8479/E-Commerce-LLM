FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ECommerce.FlashSaleOrchestrator.Domain/ECommerce.FlashSaleOrchestrator.Domain.csproj", "src/ECommerce.FlashSaleOrchestrator.Domain/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Application/ECommerce.FlashSaleOrchestrator.Application.csproj", "src/ECommerce.FlashSaleOrchestrator.Application/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Infrastructure/ECommerce.FlashSaleOrchestrator.Infrastructure.csproj", "src/ECommerce.FlashSaleOrchestrator.Infrastructure/"]
COPY ["src/ECommerce.FlashSaleOrchestrator.Api/ECommerce.FlashSaleOrchestrator.Api.csproj", "src/ECommerce.FlashSaleOrchestrator.Api/"]

RUN dotnet restore "src/ECommerce.FlashSaleOrchestrator.Api/ECommerce.FlashSaleOrchestrator.Api.csproj"

COPY src/ src/

RUN dotnet publish \
    "src/ECommerce.FlashSaleOrchestrator.Api/ECommerce.FlashSaleOrchestrator.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "ECommerce.FlashSaleOrchestrator.Api.dll"]