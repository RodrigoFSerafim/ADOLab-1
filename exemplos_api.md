# Exemplos de Uso da API ADOLab com JWT

Este arquivo contém exemplos práticos de como usar a API com autenticação JWT.

## 1. Registrar um Novo Usuário

```bash
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "joao123",
    "email": "joao@exemplo.com",
    "password": "senha123",
    "fullName": "João Silva"
  }'
```

**Resposta esperada:**
```json
{
  "id": 2,
  "username": "joao123",
  "email": "joao@exemplo.com",
  "fullName": "João Silva",
  "role": "User",
  "createdAt": "2024-01-01T10:30:00Z"
}
```

## 2. Fazer Login

```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "joao123",
    "password": "senha123"
  }'
```

**Resposta esperada:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJqb2FvMTIzIiwibmFtZSI6ImpvYW8xMjMiLCJlbWFpbCI6ImpvYW9AZXhlbXBsby5jb20iLCJyb2xlIjoiVXNlciIsImZ1bGxOYW1lIjoiSm/Do28gU2lsdmEiLCJuYmYiOjE3MDQxMTgwMDAsImV4cCI6MTcwNDEyMTYwMCwiaWF0IjoxNzA0MTE4MDAwLCJpc3MiOiJBRE9MYWIiLCJhdWQiOiJBRE9MYWJVc2VycyJ9.signature",
  "expiresAt": "2024-01-01T11:30:00Z",
  "user": {
    "id": 2,
    "username": "joao123",
    "email": "joao@exemplo.com",
    "fullName": "João Silva",
    "role": "User",
    "createdAt": "2024-01-01T10:30:00Z"
  }
}
```

## 3. Usar o Token para Acessar Endpoints Protegidos

**Salve o token da resposta anterior em uma variável:**
```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 3.1. Listar Alunos (Requer Autenticação)

```bash
curl -X GET https://localhost:7000/api/aluno \
  -H "Authorization: Bearer $TOKEN"
```

### 3.2. Inserir um Novo Aluno

```bash
curl -X POST https://localhost:7000/api/aluno \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Santos",
    "idade": 25,
    "email": "maria@exemplo.com",
    "dataNascimento": "1999-05-15T00:00:00Z"
  }'
```

### 3.3. Buscar Alunos por Propriedade

```bash
curl -X GET "https://localhost:7000/api/aluno/buscar?propriedade=nome&valor=Maria" \
  -H "Authorization: Bearer $TOKEN"
```

### 3.4. Atualizar um Aluno

```bash
curl -X PUT https://localhost:7000/api/aluno/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Santos Silva",
    "idade": 26,
    "email": "maria.silva@exemplo.com",
    "dataNascimento": "1998-05-15T00:00:00Z"
  }'
```

### 3.5. Excluir um Aluno

```bash
curl -X DELETE https://localhost:7000/api/aluno/1 \
  -H "Authorization: Bearer $TOKEN"
```

### 3.6. Obter Perfil do Usuário Atual

```bash
curl -X GET https://localhost:7000/api/aluno/me \
  -H "Authorization: Bearer $TOKEN"
```

### 3.7. Validar Token

```bash
curl -X GET https://localhost:7000/api/auth/validate \
  -H "Authorization: Bearer $TOKEN"
```

### 3.8. Atualizar Perfil do Usuário

```bash
curl -X PUT https://localhost:7000/api/auth/profile \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "João Silva Santos",
    "email": "joao.silva@exemplo.com"
  }'
```

### 3.9. Alterar Senha

```bash
curl -X PUT https://localhost:7000/api/auth/profile \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "senha123",
    "newPassword": "novasenha456"
  }'
```

## 4. Testando Sem Autenticação (Deve Falhar)

```bash
# Esta requisição deve retornar 401 Unauthorized
curl -X GET https://localhost:7000/api/aluno
```

**Resposta esperada:**
```json
{
  "message": "No authentication credentials found"
}
```

## 5. Testando com Token Inválido (Deve Falhar)

```bash
# Esta requisição deve retornar 401 Unauthorized
curl -X GET https://localhost:7000/api/aluno \
  -H "Authorization: Bearer token_invalido"
```

## 6. Exemplos com Postman

### Configuração do Environment no Postman:

1. Crie um novo Environment
2. Adicione as variáveis:
   - `base_url`: `https://localhost:7000`
   - `token`: (será preenchido automaticamente após login)

### Collection de Requisições:

#### 6.1. Auth - Register
```
POST {{base_url}}/api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "test@exemplo.com",
  "password": "test123",
  "fullName": "Usuário Teste"
}
```

#### 6.2. Auth - Login
```
POST {{base_url}}/api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "testuser",
  "password": "test123"
}
```

**Script pós-requisição para salvar o token:**
```javascript
if (pm.response.code === 200) {
    const response = pm.response.json();
    pm.environment.set("token", response.token);
}
```

#### 6.3. Aluno - Listar (Protegido)
```
GET {{base_url}}/api/aluno
Authorization: Bearer {{token}}
```

#### 6.4. Aluno - Inserir (Protegido)
```
POST {{base_url}}/api/aluno
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "nome": "Ana Costa",
  "idade": 22,
  "email": "ana@exemplo.com",
  "dataNascimento": "2002-03-10T00:00:00Z"
}
```

## 7. Testando com JavaScript/Fetch

```javascript
// Função para fazer login
async function login(username, password) {
    const response = await fetch('https://localhost:7000/api/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            usernameOrEmail: username,
            password: password
        })
    });
    
    if (response.ok) {
        const data = await response.json();
        localStorage.setItem('token', data.token);
        return data;
    } else {
        throw new Error('Login falhou');
    }
}

// Função para fazer requisições autenticadas
async function apiRequest(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    
    const response = await fetch(`https://localhost:7000${endpoint}`, {
        ...options,
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            ...options.headers
        }
    });
    
    if (response.status === 401) {
        // Token expirado ou inválido
        localStorage.removeItem('token');
        window.location.href = '/login';
        return;
    }
    
    return response;
}

// Exemplo de uso
async function listarAlunos() {
    try {
        const response = await apiRequest('/api/aluno');
        const alunos = await response.json();
        console.log(alunos);
    } catch (error) {
        console.error('Erro ao listar alunos:', error);
    }
}

// Fazer login primeiro
login('admin', 'admin123').then(() => {
    // Depois listar alunos
    listarAlunos();
});
```

## 8. Tratamento de Erros Comuns

### 8.1. Token Expirado (401)
```json
{
  "message": "Token has expired"
}
```

### 8.2. Token Inválido (401)
```json
{
  "message": "Token validation failed"
}
```

### 8.3. Usuário Não Encontrado (401)
```json
{
  "message": "Credenciais inválidas."
}
```

### 8.4. Dados Inválidos (400)
```json
{
  "message": "Nome e email são obrigatórios."
}
```

### 8.5. Usuário Já Existe (400)
```json
{
  "message": "Nome de usuário já existe."
}
```

## 9. Dicas de Debugging

### 9.1. Verificar se o Token está Válido
```bash
# Decodificar o JWT (sem verificar assinatura)
echo "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." | base64 -d
```

### 9.2. Verificar Logs da Aplicação
Os logs são salvos no arquivo `log.txt` na raiz do projeto.

### 9.3. Usar Swagger UI
Acesse https://localhost:7000/swagger para testar a API de forma interativa.

### 9.4. Verificar Certificados SSL
Se houver problemas com HTTPS, use:
```bash
curl -k https://localhost:7000/api/auth/login
```

## 10. Fluxo Completo de Exemplo

```bash
# 1. Registrar usuário
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "demo", "email": "demo@test.com", "password": "demo123", "fullName": "Demo User"}'

# 2. Fazer login e salvar token
TOKEN=$(curl -s -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usernameOrEmail": "demo", "password": "demo123"}' | jq -r '.token')

# 3. Inserir aluno
curl -X POST https://localhost:7000/api/aluno \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"nome": "Aluno Demo", "idade": 20, "email": "aluno@demo.com", "dataNascimento": "2004-01-01T00:00:00Z"}'

# 4. Listar alunos
curl -X GET https://localhost:7000/api/aluno \
  -H "Authorization: Bearer $TOKEN"
```
