# Patterns

The samples demonstrate a broad set of architectural and design patterns, spanning API design, application orchestration, domain modelling, infrastructure integration, and asynchronous messaging. The table below provides a high-level index of each pattern, the group it belongs to, and links to where it is illustrated in the layer documentation.

## Groups

| | Group | Description |
|---|---|---|
| 🌐 | **API** | Patterns governing how a domain exposes its capabilities over HTTP: endpoint design, request handling, and API-level cross-cutting concerns. |
| ⚙️ | **Application** | Patterns for orchestrating business use-cases: coordinating validation, state, mutations, and events without coupling to infrastructure details. |
| 📋 | **Contracts** | Patterns for defining and governing the shared type system that forms the public surface of a domain, consumed identically by both API and messaging consumers. |
| 🏛️ | **DDD** | Domain-Driven Design tactical patterns for modelling domains where complex rules and invariants must be enforced at the model level rather than in orchestration code. |
| 🗄️ | **Infrastructure** | Patterns for integrating with persistence stores, external systems, and technical services, keeping those concerns isolated behind clean abstractions. |
| 📨 | **Messaging** | Patterns for asynchronous, event-driven integration: producing, transporting, and consuming events reliably across domain and service boundaries. |
| 🛠️ | **Tooling** | Developer-time tools that feed the layer stack without any runtime presence: code generation from YAML configuration and schema-migration management. |
| 🧪 | **Testing** | Patterns for testing each host in isolation by distinguishing intra-domain (real) from inter-domain (mocked) dependencies, structured around host-boundary test projects. |

---

## Pattern index

| Group | | Pattern | Description | Link |
|---|---|---|---|---|
| 🌐 **API** | 🌐 | **API** | Exposing domain capabilities as HTTP endpoints via a thin controller or handler shell that delegates immediately to business logic, containing no domain rules itself. | [Hosts](hosts-layer.md#api-host) |
| | 🔑 | **Idempotency** | Ensuring that duplicate or retried requests produce the same outcome as the original, preventing unintended side effects from repeated submissions. | [Hosts](hosts-layer.md#controllers) |
| | ↔️ | **Read/Write Separation** | Dividing read and write operations into distinct service and endpoint boundaries, allowing each to evolve and scale independently. | [Hosts](hosts-layer.md#controllers) |
| ⚙️ **Application** | ✂️ | **CQRS** | Separating the write model (commands that mutate state) from the read model (queries that return data), allowing each to be designed, optimised, and scaled independently across both service and endpoint boundaries. | [Application](application-layer.md#services-and-interfaces) · [Hosts](hosts-layer.md#controllers) |
| | 🛡️ | **Policy** | A reusable guard enforcing a pre-condition that spans one or more adapter or repository calls, sitting above individual validators but below full service orchestration. | [Application](application-layer.md#policies) |
| | 🔧 | **Service** | A focused, stateless class orchestrating a single domain use-case: validating inputs, loading state, performing mutations, and raising domain events. | [Application](application-layer.md#services-and-interfaces) |
| | 🔄 | **Unit of Work** | Grouping multiple repository writes and event publications into a single atomic operation, ensuring all succeed together or none are committed. | [Application](application-layer.md#services-and-interfaces) |
| | ✅ | **Validator** | A composable rule set applied to a contract before any business logic executes. Property rules are declared fluently in the constructor (declarative phase); an optional `OnValidateAsync` override adds I/O-dependent or cross-property rules built from runtime data (programmatic phase). The two phases integrate naturally — the programmatic phase guards on prior errors to avoid unnecessary I/O. | [Application](application-layer.md#validators) |
| 📋 **Contracts** | 📄 | **Contract (DTO)** | A purpose-built, technology-agnostic type defining the exact data shape crossing a domain boundary, used identically for both API responses and messaging event payloads. | [Contracts](contracts-layer.md#entity-contracts) |
| | 🏷️ | **Reference Data** | Controlled vocabulary types modelled as first-class domain concepts rather than magic strings or plain enumerations, with built-in validity and sort semantics. | [Contracts](contracts-layer.md#reference-data) |
| 🏛️ **DDD** | 🧱 | **Aggregate** | A cluster of related entities treated as a single consistency boundary, with all mutations enforced through a root that protects invariants and tracks persistence state. | [Domain](domain-layer.md#aggregates) |
| | 🔹 | **Entity** | A domain object with a distinct, stable identity that persists across state changes, owned and tracked within an aggregate boundary. | [Domain](domain-layer.md#aggregates) |
| | 💎 | **Value Object** | An immutable, identity-free concept defined entirely by its values, enforcing its own invariants at construction and compared by value rather than by reference. | [Domain](domain-layer.md#value-objects) |
| 🗄️ **Infrastructure** | 🔀 | **Adapter** | An anti-corruption layer that wraps one or more external dependencies (HTTP clients, event publishers, local stores) behind a domain-idiomatic interface, decoupling the application from remote schemas, transports, and versioning concerns. | [Application](application-layer.md#adapters-anti-corruption-layer) · [Infrastructure](infrastructure-layer.md#external-clients-and-adapter-implementations) |
| | 🔌 | **HTTP Client** | A strongly-typed wrapper around a single outbound HTTP dependency handling serialisation, response mapping, and error translation in one focused, independently testable class. | [Infrastructure](infrastructure-layer.md#external-clients-and-adapter-implementations) |
| | 🗺️ | **Mapper** | An explicit, bidirectional translation class between two representations of the same concept — such as a domain contract and a persistence model — with no convention magic or reflection overhead. | [Infrastructure](infrastructure-layer.md#mapping) |
| | 💾 | **Persistence** | A schema-aligned model class mirroring a database table, kept deliberately separate from the domain contract so each can evolve independently without leaking database concerns upward. | [Infrastructure](infrastructure-layer.md#persistence-models) |
| | 📦 | **Repository** | A collection-like abstraction over a data store that exposes domain-idiomatic operations and return types, shielding all business logic from persistence technology choices. | [Infrastructure](infrastructure-layer.md#repositories) |
| 📨 **Messaging** | 📩 | **Event** | A domain occurrence serialised as a [CloudEvent](https://cloudevents.io), carrying a typed, versioned payload alongside standardised metadata (id, source, type, time). The same contract type is used for both API responses and message payloads; the CloudEvent envelope is applied at the transport boundary without leaking into the domain model. | [Contracts](contracts-layer.md#unified-api-and-messaging-surface) · [Hosts](hosts-layer.md#subscribe-host) |
| | 📡 | **Event-Driven Replication** | A consuming domain maintains a local, eventually-consistent copy of another domain's data by subscribing to its published events, eliminating synchronous coupling and improving resilience. | [Application](application-layer.md#adapters-anti-corruption-layer) · [Infrastructure](infrastructure-layer.md#external-clients-and-adapter-implementations) |
| | 📤 | **Outbox Relay** | A dedicated, lightweight process that polls a transactional outbox and forwards committed event records to a message broker, decoupling durable persistence from broker availability. | [Hosts](hosts-layer.md#outbox-relay-host) |
| | 📢 | **Publish** | Raising a domain event within a transactional unit of work so the event is only dispatched if the surrounding business transaction commits successfully, guaranteeing consistency. | [Application](application-layer.md#services-and-interfaces) |
| | 📥 | **Subscribe** | Declaring opt-in consumption of one or more event types via focused subscriber classes that delegate immediately to application logic, keeping business rules out of the messaging layer. | [Hosts](hosts-layer.md#subscribe-host) |
| | 📬 | **Transactional Outbox** | Persisting an outbound event to a durable outbox table in the same database transaction as the business write, guaranteeing at-least-once delivery without requiring distributed transactions or two-phase commit. | [Hosts](hosts-layer.md#outbox-relay-host) |
| 🛠️ **Tooling** | ⚙️ | **Code Generation** | Producing the full reference-data layer implementation — contract, API route, service, repository interface, repository, and mapper — from a single schema-validated YAML file, eliminating repetitive boilerplate and ensuring consistency across all layers. | [Tooling](tooling.md#code-generation-codegen) |
| | 🗃️ | **Database Management** | Managing the full database lifecycle — schema creation, DDL evolution, reference-data seeding, and outbox infrastructure provisioning — through ordered, version-controlled migration scripts and a configuration-driven tooling project. | [Tooling](tooling.md#database-management-database) |
| 🧪 **Testing** | 🔌 | **Intra-domain Testing** | Exercising a single host end-to-end against its own real infrastructure (database, cache, outbox) while replacing all cross-domain dependencies with mocks, validating the domain’s full behaviour without coupling to other services. | [Testing](testing.md#intra-domain-host-tests) |
| | 🔗 | **Inter-domain Mocking** | Replacing cross-domain HTTP calls and direct broker publishes with controlled fakes that assert the correct outbound request was made, decoupling test correctness from the availability or behaviour of other domains. | [Testing](testing.md#intra-domain-vs-inter-domain-testing) |
| | 🧹 | **Unit Testing** | Testing stateless components (validators, mappers) in a minimal DI container with no I/O, using real reference data loaded from the domain’s own seed file and mocking only intra-domain repository dependencies. | [Testing](testing.md#unit-tests-testunit) |
| | ✨ | **Local Orchestration** | Running all domain hosts simultaneously as a single distributed application with unified health, log, trace, and metric visibility, enabling cross-domain interaction and end-to-end validation in a local environment. | [Aspire](aspire.md) |

