# Decisões Técnicas

Principais escolhas de arquitetura e design, com justificativa resumida.

---

## Arquitetura

**Clean Architecture em 4 camadas** (`Domain → Application → Infrastructure → API`) com dependências apontando sempre para dentro. O domínio não conhece nenhuma outra camada — é puro C# sem dependências externas.

**CQRS leve com MediatR** — Commands e Queries separados, handlers coesos, sem a complexidade de buses ou filas. Controllers ficam finos: recebem request, montam command, devolvem resposta.

---

## Domínio

**`Order` como Aggregate Root** — `OrderItem` tem constructor `internal`, só pode ser criado por `Order.AddItem()`. Garante que nenhum invariante do pedido seja violado por código externo.

**Idempotência declarada no domínio** — `Confirm()` retorna `bool` e `Cancel()` retorna `CancelResult`. O handler apenas reage ao resultado; a lógica de "já estava confirmado" não vaza para fora do domínio.

**`Money` imutável** — operações retornam novas instâncias. Elimina bugs de referência compartilhada entre entidades.

---

## Infraestrutura

**`AppDbContext` implementa `IUnitOfWork`** — o `DbContext` já é uma implementação do padrão Unit of Work. Criar um wrapper separado seria uma abstração sem valor real.

**`AsNoTracking` cirúrgico** — queries de leitura desligam o change tracker. Queries usadas antes de escritas (`GetByIdWithItemsAsync`, `GetByIdsAsync`) mantêm tracking para que o EF detecte as mudanças.

**`GetByIdsAsync` com `WHERE id IN (...)`** — evita o problema N+1 ao carregar todos os produtos de um pedido em uma única query.

**Retry automático no Npgsql** — 3 tentativas com backoff de 5s para falhas transientes. Essencial em Docker onde a API pode tentar conectar antes do banco estar pronto.

---

## API

**Middleware global de exceções** — mapeia exceções de domínio para HTTP de forma centralizada (`DomainException → 400`, `NotFoundException → 404`, `InsufficientStockException → 409`). Controllers não tratam erros.

**`ValidationBehavior` no pipeline do MediatR** — FluentValidation roda automaticamente antes de qualquer handler. Dados inválidos nunca chegam ao domínio.

---

## Testes

**Testcontainers nos integration tests** — PostgreSQL real em Docker, não InMemory. Garante que migrations, índices e o SQL gerado pelo EF funcionam igual à produção.

**IDs fixos no seed** — produtos com GUIDs determinísticos permitem referenciá-los nos testes como constantes, sem consultas de setup.