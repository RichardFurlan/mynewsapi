# MyNewsApi

## Visão geral

**MyNewsApi** é uma API Web .NET 8 que integra com a NewsAPI (terceira‑parte) para buscar notícias, transformar respostas, persistir em PostgreSQL e expor endpoints REST com autenticação JWT. O projeto demonstra: organização por camadas (API / Application / Domain / Infra), testes unitários (xUnit + Moq + EF InMemory), documentação via Swagger, uso de Docker / docker‑compose e boas práticas de Git.

> **Nota:** Este repositório é uma solução de avaliação (coding assessment). Não inclua chaves de produção. Veja a seção de segurança.

---

## Principais funcionalidades

* Autenticação (Register / Login) usando JWT
* Buscar notícias na NewsAPI por keyword, salvar no banco e retornar resultado paginado
* Listar todas as notícias, listar notícias do usuário autenticado, listar por usuário (rota admin)
* Paginação padrão (`page`, `pageSize`) com metadados (total, pages)
* DTOs, ResultViewModel e PagedResult para respostas consistentes
* Tests: unitários para AuthService e NewsService (EF InMemory + Moq)
* Docker + docker-compose com PostgreSQL
* Swagger (Swashbuckle) com documentação dos endpoints

---

## Arquitetura do projeto

* `MyNewsApi.API` — Composição, controllers, swagger
* `MyNewsApi.Application` — Serviços, DTOs, interfaces, regras de negócio
* `MyNewsApi.Domain` — Entidades e enums de domínio
* `MyNewsApi.Infra` — DbContext, clients (NewsApi wrapper), módulo de dependências
* `tests/MyNewsApi.UnitTests` — Testes unitários

---

## Pré‑requisitos

* .NET 8 SDK
* Docker & Docker Compose (opcional, mas recomendado para execução local com PostgreSQL)
* (Opcional) Rider / VS Code / Visual Studio

---

## Variáveis de ambiente / appsettings

O projeto lê configurações de `appsettings.json` e `appsettings.Development.json`. **Nunca** comite chaves sensíveis. Variáveis importantes:

```text
ConnectionStrings:NewsDb
NewsApi:ApiKey
Jwt:Key
Jwt:Issuer
Jwt:Audience
Jwt:ExpireHours
Auth:AllowAdmin  # false para produção; true apenas para testes/dev
```

Recomendado criar um arquivo `.env` ou `appsettings.Development.json` local (não comitado) com as chaves.

---

## Executando localmente (com Docker)

1. Copie o template de env/appsettings e preencha as variáveis (ex: `appsettings.Development.json` local). NÃO comite esse arquivo.
2. Subir o PostgreSQL via docker-compose:

```bash
docker compose up -d
```

3. Criar a migration e aplicar (se necessário):

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate -p src/MyNewsApi.Infra -s src/MyNewsApi.API -o Migrations
dotnet ef database update -p src/MyNewsApi.Infra -s src/MyNewsApi.API
```

4. Rodar a API:

```bash
cd src/MyNewsApi.API
dotnet run
```

5. Acesse `http://localhost:5000/swagger` (ou porta mostrada no console) para ver a documentação.

---

## Endpoints principais

* `POST /api/auth/register` — registrar usuário
* `POST /api/auth/login` — autenticar e receber JWT
* `GET /api/news/search?keyword=xxx&page=1&pageSize=20` — busca, salva novas notícias e retorna paginado (autenticado)
* `GET /api/news/me?page=1&pageSize=20` — notícias do usuário autenticado
* `GET /api/news/all?page=1&pageSize=20` — listagem pública
* `GET /api/news/user/{userId}?page=1&pageSize=20` — listar por usuário (Admin)

Exemplos de uso (curl):

```bash
# login
curl -X POST -H "Content-Type: application/json" -d '{"email":"u@ex.com","password":"123456"}' http://localhost:5000/api/auth/login

# usar token para buscar e salvar
curl -H "Authorization: Bearer <token>" "http://localhost:5000/api/news/search?keyword=bitcoin&page=1&pageSize=10"
```

---

## Testes

Executar todos os testes:

```bash
dotnet test
```

Os testes usam `Microsoft.EntityFrameworkCore.InMemory`, `Moq` e `FluentAssertions`.

---

## CI (sugestão)

Adicionar um workflow no GitHub Actions que rode: `dotnet restore`, `dotnet build`, `dotnet test`, e `dotnet ef database update` (apenas se necessário para integration tests). Não inclua secrets no repositório; use GitHub Secrets.

---

## Boas práticas e observações finais

* Não comite chaves: se por engano comitou, remova o arquivo e **rotacione** a chave na fonte (NewsAPI). Para limpar histórico use `git filter-repo` ou `BFG Repo-Cleaner`.
* Commit history: mantenha commits pequenos e descritivos (ex.: `feat(auth): add jwt login`, `test: add newsservice unit tests`).
* Branching: use `feature/*`, `fix/*`, `chore/*`.
* Documente no README as decisões arquiteturais e pontos de melhoria.

---

## Link da API de terceiros

* NewsAPI: [https://newsapi.org/docs/endpoints/everything](https://newsapi.org/docs/endpoints/everything)

---

## Extras implementados

* Wrapper `INewsApiClient` para facilitar testes
* `ResultViewModel<T>` e `PagedResult<T>` para respostas padronizadas
* Endpoint `/api/auth/admin/promote-me` controlado por `Auth:AllowAdmin` (development‑only)

---

## Checklist antes de enviar o repositório

* [ ] Remover chaves sensíveis e arquivos com segredos
* [ ] Garantir `.gitignore` cobre `appsettings.*.local`, `.env`, `.user` etc.
* [ ] Rodar `dotnet test` e corrigir falhas
* [ ] Rodar `dotnet build`
* [ ] Atualizar `README.md` com instruções específicas de setup (já incluídas)
* [ ] Push para um branch e abrir PR (opcional)

---

## Licença

Este projeto tem licença MIT (ou escolha a que preferir).
