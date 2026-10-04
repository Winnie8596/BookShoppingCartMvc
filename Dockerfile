FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY /*.sln .
COPY BookShoppingCartMvcUI/*.csproj ./BookShoppingCartMvcUI/
# restore the web project only
RUN dotnet restore BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj

# copy everything else and build app
COPY BookShoppingCartMvcUI/. ./BookShoppingCartMvcUI/
WORKDIR /source/BookShoppingCartMvcUI
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./
# run as the non-root user, it needs to write uploaded images and the data protection keys
RUN mkdir -p /app/keys && chown -R $APP_UID /app/wwwroot/images /app/keys
USER $APP_UID

ENTRYPOINT ["dotnet", "BookShoppingCartMvcUI.dll"]
