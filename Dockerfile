FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETARCH

WORKDIR /PokeCord

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore -a $TARGETARCH --runtime linux-x64
# Build and publish a release
RUN dotnet publish "PokeCord.csproj" -a $TARGETARCH --self-contained true \
    /p:PublishTrimmed=true /p:PublishSingleFile=true -o app --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
WORKDIR /PokeCord
EXPOSE 443
COPY --from=build /PokeCord/app .

# Switch to non-root user in chiseled image
# USER app

ENV DISCORD_TOKEN=DISCORD_TOKEN

ENTRYPOINT ["./PokeCord"]
