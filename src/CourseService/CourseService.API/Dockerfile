FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CourseService.API/CourseService.API.csproj", "CourseService.API/"]
COPY ["CourseService.Application/CourseService.Application.csproj", "CourseService.Application/"]
COPY ["CourseService.Infrastructure/CourseService.Infrastructure.csproj", "CourseService.Infrastructure/"]
COPY ["CourseService.Domain/CourseService.Domain.csproj", "CourseService.Domain/"]

RUN dotnet restore "CourseService.API/CourseService.API.csproj"

COPY . .

WORKDIR "/src/CourseService.API"
RUN dotnet build "CourseService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CourseService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CourseService.API.dll"]
