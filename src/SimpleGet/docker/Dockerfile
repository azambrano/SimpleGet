FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
COPY . /app
RUN ls app
WORKDIR /app/CV.FNMA.Api
RUN dotnet clean
RUN dotnet restore
RUN dotnet publish -c Release -o publish

# build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
WORKDIR /app
COPY --from=build-env /app/SimpleGet/publish .
EXPOSE 9021
ENV ASPNETCORE_URLS "http://*:9021"
CMD dotnet SimpleGet.dll