FROM mcr.microsoft.com/dotnet/sdk:8.0.403 as build
WORKDIR /app

COPY Pkmds.sln ./
COPY ./Pkmds.Web/Pkmds.Web.csproj ./Pkmds/

# RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

FROM nginx:1.27-alpine
WORKDIR /app
EXPOSE 8080
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/out/wwwroot /usr/share/nginx/html