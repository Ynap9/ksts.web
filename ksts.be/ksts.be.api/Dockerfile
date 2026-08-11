# See https://aka.ms/customizecontainer to learn how to customize your debug container.
# Build context là ksts.be (thư mục cha), vì project này tham chiếu các project anh em nằm ngoài nó:
#   docker build -f ksts.be.api/Dockerfile -t ksts-be:latest .

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# tzdata cho múi giờ "SE Asia Standard Time"; fonts-liberation + fontconfig để vẽ khối chữ ký (Times New Roman).
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata fonts-liberation fontconfig \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ksts.be.api/ksts.be.api.csproj", "ksts.be.api/"]
COPY ["ksts.be.applications/ksts.be.applications.csproj", "ksts.be.applications/"]
COPY ["ksts.be.domain/ksts.be.domain.csproj", "ksts.be.domain/"]
COPY ["ksts.be.external/ksts.be.external.csproj", "ksts.be.external/"]
COPY ["ksts.be.infrastructure/ksts.be.infrastructure.csproj", "ksts.be.infrastructure/"]
COPY ["ksts.be.external/ksts.be.external.csproj", "ksts.be.external/"]
COPY ["ksts.be.shared/ksts.be.shared.csproj", "ksts.be.shared/"]
RUN dotnet restore "./ksts.be.api/ksts.be.api.csproj"
COPY . .
WORKDIR "/src/ksts.be.api"
RUN dotnet build "./ksts.be.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ksts.be.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ksts.be.api.dll"]
