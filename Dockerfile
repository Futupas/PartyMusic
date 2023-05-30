# Base image with .NET SDK 8 and VLC
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base

# Set the working directory
WORKDIR /app

# Copy the project files to the container
COPY ./src .

# Restore dependencies and build the application
RUN dotnet restore && \
    dotnet build --no-restore

# Expose port 82 on the container
EXPOSE 82

# Set the entry point for the container
ENTRYPOINT ["dotnet", "run", "--urls", "http://0.0.0.0:82"]
