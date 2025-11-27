FROM mcr.microsoft.com/dotnet/runtime:9.0

COPY deploy/ /app

WORKDIR /app
CMD /app/Spend