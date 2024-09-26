# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BASE=

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0$BASE AS base
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER $APP_UID
WORKDIR /app
RUN mkdir ./data
EXPOSE 8080
VOLUME /app/data

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ARG VERSION=0.0.1
WORKDIR /src
COPY ["Asilifelis/Asilifelis.csproj", "Asilifelis/"]
RUN dotnet restore "./Asilifelis/Asilifelis.csproj"
COPY . .
WORKDIR "/src/Asilifelis"
RUN dotnet build "./Asilifelis.csproj" \
	-c $BUILD_CONFIGURATION \
	-o /app/build \
    -p:Version="${VERSION}"

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Asilifelis.csproj" \
	-c $BUILD_CONFIGURATION \
	-o /app/publish \
	-p:UseAppHost=false \
	-p:Version="${VERSION}"

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Asilifelis.dll"]