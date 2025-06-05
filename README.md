# 🏗️ Clean Architecture Microservice Template

Um template .NET avançado para criação de microserviços seguindo **Clean Architecture**, **Hexagonal Architecture (Ports & Adapters)**, princípios **SOLID**, **DRY**, **KISS** e **Object Calisthenics** com foco em **alta performance** e **baixo uso de memória**.

## 🎯 Características Principais

### 🏛️ Arquitetura
- **Clean Architecture** com separação clara de responsabilidades
- **Hexagonal Architecture** (Ports & Adapters) para isolamento de dependências
- **CQRS** com mediator pattern para separação de comandos e consultas
- **Result Pattern** para tratamento de erros sem exceptions (melhor performance)
- **Domain-Driven Design** com value objects e entidades ricas

### ⚡ Performance & Otimizações
- **Pool de conexões** para bancos de dados
- **Object pooling** para objetos caros de criar
- **Async/await** otimizado para alta concorrência
- **Memory-efficient** patterns e structs quando apropriado
- **Server GC** habilitado para throughput máximo
- **Compilation otimizada** com ReadyToRun e Trimming
- **Response compression** (Gzip/Brotli)

### 🛡️ Qualidade de Código
- **Princípios SOLID** aplicados consistentemente
- **Object Calisthenics** para código mais limpo
- **DRY** (Don't Repeat Yourself) eliminando duplicação
- **KISS** (Keep It Simple, Stupid) priorizando simplicidade
- **Validação robusta** com validators dedicados
- **Testes unitários** preparados

### 🔧 Tecnologias Suportadas

#### 💾 Bancos de Dados
- **SQL Server** com otimizações de performance
- **PostgreSQL** com driver Npgsql otimizado
- **MongoDB** com padrões NoSQL

#### 📨 Mensageria
- **Apache Kafka** com alta performance
- **RabbitMQ** otimizado para throughput

#### 🚀 Cache & Performance
- **Redis** para cache distribuído
- **In-memory cache** otimizado
- **Response compression**
- **Rate limiting** para proteção

#### 📊 Observabilidade
- **OpenTelemetry** para tracing distribuído
- **Prometheus metrics** para monitoramento
- **Structured logging** com Serilog
- **Health checks** detalhados

## 🚀 Instalação e Uso

### 📋 Pré-requisitos
- .NET 8.0 ou superior
- Docker (opcional, para containerização)

### 🎯 Instalação do Template

```bash
# Instalar o template
dotnet new install ./

# Verificar instalação
dotnet new list | grep cleanarch
```

### 🛠️ Criando um Microserviço

#### Básico (SQL Server)
```bash
dotnet new cleanarch.api -n MeuMicroservico
```

#### Com PostgreSQL
```bash
dotnet new cleanarch.api -n MeuMicroservico --database-type postgresql
```

#### Com MongoDB
```bash
dotnet new cleanarch.api -n MeuMicroservico --database-type mongodb
```

#### Com Mensageria (Kafka + RabbitMQ)
```bash
dotnet new cleanarch.api -n MeuMicroservico --use-kafka --use-rabbitmq
```

#### Com Redis Cache
```bash
dotnet new cleanarch.api -n MeuMicroservico --use-redis
```

#### Configuração Completa
```bash
dotnet new cleanarch.api -n MeuMicroservico \
  --database-type postgresql \
  --use-kafka \
  --use-rabbitmq \
  --use-redis \
  --use-metrics \
  --use-health-checks \
  --include-docker-support \
  --include-k8s-manifests
```

### 📋 Parâmetros Disponíveis

| Parâmetro | Valores | Padrão | Descrição |
|-----------|---------|--------|-----------|
| `--database-type` | `none`, `sqlserver`, `postgresql`, `mongodb` | `sqlserver` | Tipo de banco de dados |
| `--use-kafka` | `true`, `false` | `false` | Incluir Apache Kafka |
| `--use-rabbitmq` | `true`, `false` | `false` | Incluir RabbitMQ |
| `--use-redis` | `true`, `false` | `false` | Incluir Redis cache |
| `--use-metrics` | `true`, `false` | `true` | Incluir métricas OpenTelemetry |
| `--use-health-checks` | `true`, `false` | `true` | Incluir health checks |
| `--use-swagger` | `true`, `false` | `true` | Incluir Swagger/OpenAPI |
| `--use-jwt-auth` | `true`, `false` | `true` | Incluir autenticação JWT |
| `--use-result-pattern` | `true`, `false` | `true` | Usar Result Pattern vs Exceptions |
| `--include-docker-support` | `true`, `false` | `true` | Incluir Dockerfile otimizado |
| `--include-k8s-manifests` | `true`, `false` | `false` | Incluir manifestos Kubernetes |
| `--framework` | `net8.0`, `net9.0` | `net8.0` | Framework target |
| `--version` | string | `1.0.0` | Versão inicial |

## 🏗️ Estrutura do Projeto

```
📁 MeuMicroservico/
├── 📁 src/
│   └── 📁 microservice.api/
│       ├── 📁 Domain/
│       │   ├── 📁 Core/
│       │   │   ├── 📁 Base/           # Classes base e interfaces fundamentais
│       │   │   ├── 📁 Interfaces/    # Interfaces de domínio
│       │   │   ├── 📁 Models/        # Entidades, VOs e DTOs
│       │   │   ├── 📁 Mediator/      # Implementação do mediator
│       │   │   └── 📁 Settings/      # Configurações de domínio
│       │   ├── 📁 UseCases/          # Casos de uso da aplicação
│       │   └── 📁 Services/          # Serviços de domínio
│       ├── 📁 Adapters/
│       │   ├── 📁 Inbound/           # Adaptadores de entrada
│       │   │   ├── 📁 WebApi/        # Controllers e endpoints
│       │   │   └── 📁 Middleware/    # Middlewares personalizados
│       │   └── 📁 Outbound/          # Adaptadores de saída
│       │       ├── 📁 Database/      # Repositórios SQL/NoSQL
│       │       ├── 📁 Messaging/     # Kafka, RabbitMQ
│       │       ├── 📁 Cache/         # Redis, Memory Cache
│       │       ├── 📁 Logging/       # Logging estruturado
│       │       └── 📁 Metrics/       # Coleta de métricas
│       ├── 📁 Configurations/        # DI e configurações
│       ├── 📄 Program.cs             # Entry point otimizado
│       └── 📄 microservice.api.csproj
├── 📁 k8s/                          # Manifestos Kubernetes (opcional)
├── 📄 Dockerfile                     # Container otimizado
├── 📄 docker-compose.yml            # Compose para desenvolvimento
└── 📄 README.md
```

## 🎯 Princípios Implementados

### 🏛️ SOLID
- **S**ingle Responsibility: Cada classe tem uma única responsabilidade
- **O**pen/Closed: Extensível sem modificação
- **L**iskov Substitution: Substituição de tipos sem quebrar funcionalidade
- **I**nterface Segregation: Interfaces específicas e focadas
- **D**ependency Inversion: Dependência de abstrações, não implementações

### 🎨 Object Calisthenics
- ✅ Apenas um nível de indentação por método
- ✅ Não usar palavra-chave `else`
- ✅ Encapsular tipos primitivos em Value Objects
- ✅ Coleções como objetos de primeira classe
- ✅ Apenas um ponto por linha
- ✅ Não abreviar nomes
- ✅ Manter entidades pequenas
- ✅ Máximo duas variáveis de instância por classe
- ✅ Sem getters/setters desnecessários

### 🎯 Clean Code
- **DRY**: Eliminação de duplicação de código
- **KISS**: Simplicidade sobre complexidade
- **YAGNI**: Implementar apenas o necessário
- **Nomes descritivos** e intenção clara
- **Funções pequenas** e focadas
- **Comentários apenas quando necessário**

## 🚀 Performance Features

### ⚡ Otimizações de Runtime
- **Server GC** para máximo throughput
- **Tiered Compilation** para startup rápido
- **ReadyToRun** images para performance
- **Assembly trimming** para containers menores

### 💾 Gestão de Memória
- **Object pooling** para objetos caros
- **Struct** para value types pequenos
- **Span<T>** e **Memory<T>** para manipulação eficiente
- **ArrayPool** para arrays temporários

### 🔄 Async/Await Otimizado
- **ConfigureAwait(false)** consistente
- **ValueTask** quando apropriado
- **Cancellation tokens** em todos async methods
- **Task.Run** apenas quando necessário

### 🌐 HTTP & Networking
- **Connection pooling** otimizado
- **Keep-alive** configurado
- **Compression** habilitada (Gzip/Brotli)
- **Rate limiting** para proteção

## 📊 Monitoramento e Observabilidade

### 📈 Métricas
- **Request duration** e **throughput**
- **Memory usage** e **GC metrics**
- **Database connection** metrics
- **Custom business** metrics

### 🔍 Logging
- **Structured logging** com Serilog
- **Correlation IDs** para rastreamento
- **Log levels** otimizados por ambiente
- **Performance logging** para requests lentos

### 🩺 Health Checks
- **Liveness** probes para Kubernetes
- **Readiness** probes para load balancers
- **Dependency** health checks (DB, Cache, etc.)
- **Custom** health checks para business logic

## 🐳 Docker & Kubernetes

### 🐳