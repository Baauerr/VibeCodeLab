# VibeCodingLab — Application

This folder contains the ASP.NET Core `Mockups` application.

How to run:

1. Start SQL Server only (we provide a docker-compose):

```powershell
cd Application
docker-compose up -d sqlserver
```

2. To run locally against the container DB (example):

Set the environment variable `ConnectionStrings__DefaultConnection` to `Server=127.0.0.1,1433;Database=Backend3DB;User Id=sa;Password=YourStrongPassword123!;Encrypt=false;` and run from Visual Studio or `dotnet run`.

Load tests are in `load-test/` (k6 script).
