# Minimal API in ASP.NET 6

```bash
docker run -it mcr.microsoft.com/dotnet/sdk:6.0-alpine sh
/ # dotnet --info
/ # dotnet new web
```

```bash
docker build -t hello-net6 .
docker run -it --rm -p 8080:80 hello-net6
```
