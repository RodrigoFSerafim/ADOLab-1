# ADOLab com JWT Authentication

Este projeto implementa autenticação JWT (JSON Web Token) em uma aplicação C# com ASP.NET Core, ADO.NET e SQL Server.

## Funcionalidades Implementadas

### 🔐 Autenticação JWT
- **Registro de usuários**: Criação de contas com senhas criptografadas
- **Login**: Autenticação com JWT tokens
- **Validação de tokens**: Middleware para proteger endpoints
- **Gestão de perfis**: Atualização de dados do usuário

### 📊 CRUD de Alunos (Protegido)
- **Listagem**: Visualizar todos os alunos (requer autenticação)
- **Busca**: Pesquisar alunos por propriedade
- **Inserção**: Adicionar novos alunos
- **Atualização**: Modificar dados existentes
- **Exclusão**: Remover alunos

### 🗄️ Banco de Dados
- **Tabela Users**: Armazenamento de usuários com roles
- **Tabela Alunos**: Dados dos estudantes
- **Criptografia**: Senhas protegidas com BCrypt

## Como Executar

### 1. Pré-requisitos
- .NET 8.0 SDK
- SQL Server (LocalDB ou Express)
- Visual Studio ou VS Code

### 2. Configuração do Banco de Dados
Atualize a connection string no `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=localhost;Database=ADOLabDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 3. Executar a Aplicação
```bash
dotnet run
```

A aplicação irá:
- Criar automaticamente as tabelas no banco de dados
- Criar um usuário administrador padrão
- Iniciar o servidor web na porta HTTPS
- Abrir o menu interativo no console

### 4. Acessar a API
- **Swagger UI**: https://localhost:7000/swagger
- **API Base**: https://localhost:7000/api

## Usuário Administrador Padrão
- **Username**: `admin`
- **Password**: `admin123`
- **Email**: `admin@adolab.com`
- **Role**: `Admin`

## Endpoints da API

### Autenticação (AuthController)
```
POST /api/auth/login
POST /api/auth/register
GET  /api/auth/profile (requer autenticação)
PUT  /api/auth/profile (requer autenticação)
GET  /api/auth/validate (requer autenticação)
```

### Alunos (AlunoController) - Requer Autenticação
```
GET    /api/aluno
GET    /api/aluno/buscar?propriedade=nome&valor=João
POST   /api/aluno
PUT    /api/aluno/{id}
DELETE /api/aluno/{id}
GET    /api/aluno/me (informações do usuário atual)
```

## Como Usar com JWT

### 1. Fazer Login
```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "admin",
    "password": "admin123"
  }'
```

Resposta:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@adolab.com",
    "fullName": "Administrador",
    "role": "Admin",
    "createdAt": "2024-01-01T10:00:00Z"
  }
}
```

### 2. Usar o Token em Requisições
```bash
curl -X GET https://localhost:7000/api/aluno \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 3. Registrar Novo Usuário
```bash
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "novousuario",
    "email": "usuario@exemplo.com",
    "password": "senha123",
    "fullName": "Novo Usuário"
  }'
```

## Configurações JWT

As configurações JWT estão no `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "ADOLab_Super_Secret_Key_For_JWT_Token_Generation_2024_Minimum_32_Characters",
    "Issuer": "ADOLab",
    "Audience": "ADOLabUsers",
    "ExpirationMinutes": 60
  }
}
```

### Segurança
- **SecretKey**: Deve ter pelo menos 32 caracteres em produção
- **ExpirationMinutes**: Tempo de vida do token (60 minutos)
- **Issuer/Audience**: Identificadores únicos da aplicação

## Estrutura do Projeto

```
ADOLab/
├── ADOLab.csproj          # Dependências do projeto
├── Program.cs             # Configuração da aplicação e JWT
├── appsettings.json       # Configurações e connection string
├── User.cs                # Modelos de usuário e DTOs
├── UserRepository.cs      # Repositório para operações de usuário
├── JwtService.cs          # Serviço de geração e validação JWT
├── AuthController.cs      # Controlador de autenticação
├── AlunoController.cs     # Controlador CRUD de alunos (protegido)
├── Aluno.cs               # Modelo de aluno
├── AlunoRepository.cs     # Repositório para operações de aluno
├── IRepository.cs         # Interface do repositório
└── FileLogger.cs          # Sistema de logging
```

## Segurança Implementada

### 🔒 Criptografia
- **Senhas**: Hash BCrypt com salt automático
- **Tokens**: Assinados com HMAC SHA256
- **Validação**: Verificação de assinatura, emissor, audiência e expiração

### 🛡️ Validações
- **Input validation**: Validação de dados de entrada
- **SQL Injection**: Proteção com parâmetros tipados
- **Token validation**: Verificação de claims e expiração
- **Role-based**: Sistema de roles (User/Admin)

### 📝 Logging
- **Auditoria**: Log de todas as operações de autenticação
- **Erros**: Registro detalhado de exceções
- **Performance**: Log de operações de banco de dados

## Menu Interativo

A aplicação mantém um menu console para operações CRUD:
```
=== CRUD ADO.NET – Alunos ===
1) Inserir
2) Listar
3) Editar
4) Deletar
5) Buscar
6) Usuários
0) Sair
```

## Próximos Passos

Para produção, considere:
1. **HTTPS obrigatório**: Certificados SSL válidos
2. **Secrets management**: Azure Key Vault ou similar
3. **Rate limiting**: Proteção contra ataques de força bruta
4. **Refresh tokens**: Renovação automática de tokens
5. **Auditoria**: Logs de segurança mais detalhados
6. **CORS**: Configuração adequada para frontend
7. **Health checks**: Monitoramento da aplicação

## Tecnologias Utilizadas

- **.NET 8.0**: Framework principal
- **ASP.NET Core**: Web API
- **JWT Bearer**: Autenticação
- **BCrypt.Net**: Criptografia de senhas
- **ADO.NET**: Acesso a dados
- **SQL Server**: Banco de dados
- **Swagger**: Documentação da API
