# 🕒 PontoApp  
> Sistema de registro e controle de ponto — desenvolvido em **ASP.NET Core MVC** com arquitetura **Clean-ish**

---

## 📦 Estrutura da Solução

A solução contém 4 projetos principais, organizados conforme o princípio de separação de responsabilidades:

| Projeto | Responsabilidade |
|----------|------------------|
| **PontoApp.Domain** | Entidades, enums e interfaces de repositórios. |
| **PontoApp.Application** | Casos de uso, serviços de aplicação e DTOs. |
| **PontoApp.Infrastructure** | Persistência de dados (EF Core, repositórios e `DbContext`). |
| **PontoApp.Web** | Aplicação MVC (camada de apresentação, autenticação, controllers e views). |

---

## ⚙️ Montando a Solução do Zero

Abra um terminal **na pasta raiz** (onde está este README) e execute:

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

> ⚠️ Os arquivos deste repositório **já substituem** os gerados pelos comandos acima.  
> Se o editor perguntar para sobrescrever, **confirme**.

---

## 🧩 Banco de Dados

1. Ajuste a connection string em  
   `PontoApp.Web/appsettings.json`  
   conforme sua instância local (ex.: `localhost\SQLEXPRESS`).

2. Crie as migrações e atualize o banco:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial -p PontoApp.Infrastructure -s PontoApp.Web
dotnet ef database update -p PontoApp.Infrastructure -s PontoApp.Web
```

3. Popular as colaboradoras iniciais (via **SSMS** ou **sqlcmd**):

```sql
INSERT INTO Employees (Nome, Email, Pin, Ativo)
VALUES
('Colaboradora A', 'a@empresa.com', '1234', 1),
('Colaboradora B', 'b@empresa.com', '5678', 1);
```

---

## 🚀 Executar o Projeto

```powershell
dotnet run --project PontoApp.Web
```

Acesse no navegador:  
👉 [https://localhost:5001/Punch](https://localhost:5001/Punch)

---

## 🔐 Autenticação e Autorização

O sistema usa **Cookie Authentication** com **roles** baseadas em `Claims`:

- **Admin** – e-mail configurado em `appsettings.json`:
  ```json
  "Auth": {
    "MasterAdminEmail": "admin@empresa.com"
  }
  ```
- **Employee** – usuários vinculados a um `EmployeeId` válido.

### Fluxos de Acesso

| Função | Caminho | Requisitos |
|---------|----------|-------------|
| **Login** | `/Account/Login` | Qualquer usuário |
| **Cadastro de Admin (primeiro uso)** | `/Account/SetupAdmin` | Apenas se não houver usuários no banco |
| **Cadastro de Colaborador** | `/Account/RegisterEmployee` | PIN válido e colaborador ativo |
| **Cadastro de Usuário (Admin)** | `/Account/Register` | Restrito à policy `"RequireAdmin"` |
| **Esqueci minha senha / Redefinição** | `/Account/ForgotPassword` / `/Account/ResetPassword` | Acesso público |

O sistema aplica **status HTTP adequados** (400, 401, 403, 404, 409, 422, 500) para cada cenário.

---

## ⚙️ Camadas e Responsabilidades

### 🧱 `PontoApp.Domain`
- Entidades: `AppUser`, `Employee`, `Punch`
- Interfaces: `IUserRepository`, `IEmployeeRepository`, `IPunchRepository`
- Contratos e enums base

### 💡 `PontoApp.Application`
- Casos de uso e serviços (`PunchService`, `WorkSummaryDto`, etc.)
- Regras de validação de ponto (impede `IN` duplo ou `OUT` duplo)
- Utilitários (`PasswordHasher`, `IEmailSender`)

### 💾 `PontoApp.Infrastructure`
- `PontoDbContext` (EF Core)
- Repositórios concretos
- Migrations e configurações de timezone (`America/Sao_Paulo`)

### 🌐 `PontoApp.Web`
- Controllers MVC (`AccountController`, `PunchController`, `HomeController`)
- ViewModels com DataAnnotations
- Views Razor (Bootstrap 5)
- Configuração de autenticação com cookies

---

## 🧠 Lógica de Negócio

- Impede múltiplos registros de **entrada** ou **saída** consecutivos  
- Converte horários para o fuso horário **America/Sao_Paulo**  
- Gera **resumo de horas trabalhadas** por período (`WorkSummaryDto`)  
- Envia código de redefinição de senha por e-mail com validade de **15 minutos**  
- Armazena códigos de redefinição criptograficamente seguros  
- Separa fluxo de **Admin** e **Employee** por role e policy  

---

## 🧱 Próximos Passos

- [ ] Exportação de relatórios (CSV / Excel)  
- [ ] Dashboard de presença  
- [ ] Integração com ASP.NET Core Identity  
- [ ] Suporte mobile (Blazor Hybrid / MAUI)  
- [ ] Logs e auditoria detalhados  

---

## 🕓 Timezone

Usa **America/Sao_Paulo** (ou `E. South America Standard Time` no Windows).

---
