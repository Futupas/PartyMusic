# VLC
# FROM galexrt/vlc:latest
FROM jrottenberg/ffmpeg:4.1-alpine-vlc


# Base image with .NET SDK 8 and VLC
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base

# Set an environment variable for VLC installation path
ENV VLC_DIR /usr/lib/vlc

# Install VLC dependencies
# RUN apt update
# RUN apt upgrade -y
# RUN apt install vlc -y

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
