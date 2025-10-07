# PontoApp (MVC + Clean-ish Architecture)

Solução com 4 projetos:
- **PontoApp.Domain** (entidades e interfaces de repositório)
- **PontoApp.Application** (casos de uso/serviços e DTOs)
- **PontoApp.Infrastructure** (EF Core, DbContext e repositórios)
- **PontoApp.Web** (ASP.NET Core MVC)

## Como montar a solução rapidamente

Abra um terminal na pasta raiz (onde está este README) e execute:

```powershell
dotnet new sln -n PontoApp
dotnet new classlib -n PontoApp.Domain
dotnet new classlib -n PontoApp.Application
dotnet new classlib -n PontoApp.Infrastructure
dotnet new mvc -n PontoApp.Web -f net8.0

dotnet sln add PontoApp.Domain PontoApp.Application PontoApp.Infrastructure PontoApp.Web

dotnet add PontoApp.Application reference PontoApp.Domain
dotnet add PontoApp.Infrastructure reference PontoApp.Domain
dotnet add PontoApp.Web reference PontoApp.Application
dotnet add PontoApp.Web reference PontoApp.Infrastructure

dotnet add PontoApp.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add PontoApp.Infrastructure package Microsoft.EntityFrameworkCore.Tools
dotnet add PontoApp.Web package Microsoft.AspNetCore.Authentication.Cookies
```

> Os arquivos deste zip **já** sobrescrevem os arquivos criados pelos comandos acima. Se o editor perguntar para substituir, confirme.

## Banco de Dados

Edite a connection string em `PontoApp.Web/appsettings.json` conforme sua instância (ex.: `localhost\SQLEXPRESS`).

Crie as migrações e atualize o banco:
```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial -p PontoApp.Infrastructure -s PontoApp.Web
dotnet ef database update -p PontoApp.Infrastructure -s PontoApp.Web
```

Popular as duas colaboradoras (via SSMS ou `sqlcmd`):
```sql
INSERT INTO Employees (Nome, Email, Pin, Ativo) VALUES
('Colaboradora A','a@empresa.com','1234',1),
('Colaboradora B','b@empresa.com','5678',1);
```

Executar:
```powershell
dotnet run --project PontoApp.Web
```
Acesse `https://localhost:5001/Punch`.

## Observações
- Tempo local: usa America/Sao_Paulo (Windows: `E. South America Standard Time`; Linux: `America/Sao_Paulo`).
- Regras básicas: não permite dois `IN` seguidos ou dois `OUT` seguidos.
- Próximos passos: relatórios por período, exportação CSV, ASP.NET Core Identity.
