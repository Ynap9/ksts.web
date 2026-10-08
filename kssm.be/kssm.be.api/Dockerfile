# See https://aka.ms/customizecontainer to learn how to customize your debug container.
# Build context là kssm.be (thư mục cha), vì project này tham chiếu các project anh em nằm ngoài nó:
#   docker build -f kssm.be.api/Dockerfile -t kssm-be:latest .

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# tzdata cho múi giờ "SE Asia Standard Time"; ttf-mscorefonts-installer để khối chữ ký vẽ đúng Times New Roman
# như TT05 Điều 8 đòi (nằm ở kho contrib, phải nhận EULA trước). fonts-liberation giữ làm dự phòng.
# Gói này tải font từ Internet lúc build và có thể cài "thành công" mà không có file nào, nên kiểm fc-list để
# build hỏng ngay thay vì âm thầm vẽ bằng font khác.
USER root
RUN sed -i 's/^Components: main$/Components: main contrib/' /etc/apt/sources.list.d/debian.sources \
    && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
    && apt-get update \
    && apt-get install -y --no-install-recommends tzdata ca-certificates fonts-liberation fontconfig ttf-mscorefonts-installer \
    && fc-cache -f \
    && fc-list | grep -qi "Times New Roman" \
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
COPY ["kssm.be.api/kssm.be.api.csproj", "kssm.be.api/"]
COPY ["kssm.be.applications/kssm.be.applications.csproj", "kssm.be.applications/"]
COPY ["kssm.be.domain/kssm.be.domain.csproj", "kssm.be.domain/"]
COPY ["kssm.be.external/kssm.be.external.csproj", "kssm.be.external/"]
COPY ["kssm.be.infrastructure/kssm.be.infrastructure.csproj", "kssm.be.infrastructure/"]
COPY ["kssm.be.shared/kssm.be.shared.csproj", "kssm.be.shared/"]
RUN dotnet restore "./kssm.be.api/kssm.be.api.csproj"
COPY . .
WORKDIR "/src/kssm.be.api"
RUN dotnet build "./kssm.be.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./kssm.be.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kssm.be.api.dll"]
