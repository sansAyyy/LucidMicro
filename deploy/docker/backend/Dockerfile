# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ARG PROJECT_PATH

COPY backend ./backend

RUN test -n "$PROJECT_PATH"
RUN dotnet restore "$PROJECT_PATH"
RUN dotnet publish "$PROJECT_PATH" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ARG APP_DLL
ENV APP_DLL=$APP_DLL
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "if [ -z \"$APP_DLL\" ]; then echo 'APP_DLL is required.'; exit 1; fi; exec dotnet \"$APP_DLL\""]
