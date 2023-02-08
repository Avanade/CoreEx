# CoreEx.Entities

The `CoreEx.Entities` namespace is a key namespace used for the definition of entities and/or models to enable additional and extended capabilities.

<br/>

## Motivation

Entities ([aggregate roots](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model#the-domain-entity-pattern) (i.e. [DDD](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)), [value-objects](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects), [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object)s, whatever in your parlance) and [data models](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) play a key role within an application architecture. They are often implemented using a pattern of sorts to standardized functionality, implementation, etc. This namespace provides a number of common/standard capabilities that can be leveraged to standardize and create functionally richer entities and/or data models (where they leverage these features).

<br/>

## Entity key

Where an entity needs to be uniquely identified a key of some description is required; it will consist of one or more values. The [`CompositeKey`](./CompositeKey.cs) struct provides a standardized representation of the key value, with a corresponding [`IEntityKey`](./IEntityKey.cs) interface enabling the base `CompositeKey` value access.

However, in practice one of the following two interfaces should be used to enable the key:

Interface | Description
-|-
`IIdentifier<TId>` | Enables a single `Id` property. This is the most common means to name and represent a single identifier value; the underlying type can be a `string`, `int`, `Guid`, etc.
`IPrimaryKey` | Enables a primary key that can consist of one or more values, represented as a `CompositeKey`. The values that represent the composite key must in turn be represented as properties for the entity as the underlying composite`IPrimaryKey.PrimaryKey` property is read-only.

Additionally, there is an [`EntityKeyCollection`](./EntityKeyCollection.cs) that supports objects that implement `IEntityKey`. This collection does _not_ manage/guarantee uniqueness by design, instead provides an `IsAnyDuplicates` to identify. Where uniqueness is required, then some form of `IDictionary` should be leveraged instead.

<br/>

### Identifier generation

Where an identfier value needs to be _generated_ at runtime the following enable:

Type | Description
-|-
[`IIdentifierGenerator<TId>`](./IIdentifierGeneratorT.cs) | Provides the logic to generate a new identifier for a specified `Id` type.
[`IIdentifierGenerator`](./IIdentifierGenerator.cs) | Provides the logic to generate an identifier for an entity value.
[`IdentifierGenerator`](./IdentifierGenerator.cs) | Provides the default logic to generate a `string` or `Guid` idenfifier (from a new `Guid`).

Where an alternate identifier generation is required, for example [nanoid](https://github.com/codeyu/nanoid-net), then a new [`IIdentifierGenerator`](./IIdentifierGenerator.cs) implementation will be required.

<br/>

## Paging

Within an application, where dealing with entity collections, a standardized approach to paging may be desired. The [`PagingArgs`](./PagingArgs.cs) provides a standard means to specify the `Page` and `Size`, or alternative `Skip` and `Take`; with an additional request to get the _total count_ (`IsGetCount`). There is a corresponding [`PagingResult`](./PagingResult.cs) that is intended to house the resulting `TotalCount` where requested.

The [`ICollectionResult`](./ICollectionResult.cs) interface and corresponding [`CollectionResult`](./CollectionResult.cs) implementation provide a standardized means to capture the result of the likes of a paged query, to access the `Paging` result and underlying collection `Items`. The [WebApis](../WebApis) namespace leverages this to manage the serialization of this result in a consistent manner.

<br/>

## Cleaning

An entity and its properties, generatlly where represented as a POCO, may contain state that is not considered consistent and is a candidate for cleaning; being the replacement (change) of the property where it meets one of the generic conditions to cleanse. This does not imply post this that an entity should be considered in a final valid state, only specific validation logic can ascertain this; however, at least some basic value assumptions can be made as a result.

The [`Cleaner`](./Cleaner.cs) class enables the cleansing logic. Provides `Clean` methods to cleanse the following .NET types as follows:

Type | Description
- | -
`string` | Cleans using following: <br/>&#8226; [`StringTrim`](./StringTrim.cs): `None`, `Both`, `Start` or `End` (default). <br/>&#8226;  [`StringTransform`](./StringTransform.cs): `None`, `NullToEmpty` or `EmptyToNull` (default). <br/>&#8226;  [`StringCase`](./StringCase.cs): `None` (default), `Lower`, `Upper` or `Title`.
`DateTime` | Cleans using [`DateTimeTransform`](./DateTimeTransform.cs): `None`, `DateOnly`, `DateTimeLocal`, `DateTimeUtc (default)`, or `DateTimeUnspecified`.
[`IInitial`](./IInitial.cs) | Provides a means to determine if the value is in its initial/default (`IsInitial`) state and therefore the reference to this should be set to `null`. This is essentially the equivalent of the `StringTransform.EmptyToNull`, but for an object.
[`ICleanUp`](./ICleanUp.cs) | Enables additional cleansing for the object instance by invoking the `CleanUp` method where implemented.

</br>

## Change audit

This relates to the standardized change auditing, being the capture of the respective create and update, user and timestamp. The [`IChangeLogAudit`](./IChangeLogAudit.cs) interface defines the standard properties, with the [`ChangeLog`](./ChangeLog.cs) class representing an implementation. 

Within an implementation the base [`IChangeLogAuditLog`](./IChangeLogAuditLog.cs) interface, generally accessed via the [`IChangeLog`](./IChangeLog.cs) interface, enables access to the underlying `ChangeLog` property.

Additionally, the [`ChangeLog`](./ChangeLog.cs) class provides static methods to either `PrepareCreated` or `PrepareUpdated` to set the underlying properties accordingly. This is the preferred method to perform this action.

</br>

## Messages

The [`MessageItem`](./MessageItem.cs) and corresponding [`MessageItemCollection`](./MessageItemCollection.cs) provide a _CoreEx_-wide standardized means to define and manage messages; including a [`MessageType`](./MessageType.cs) (`Error`, `Warning` or `Info`) and `Text`, with optional `Property` for usage by the likes of [Validation](../Validation) where applicable.

A number of static `Create` methods are also provided to simplify the creation leveraging [composite formatting](https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting) and [`LText`](../Localization/LText.cs) (for localization and/or multilingual).

<br/>

## Additional capabilities

The following enable additional ad-hoc capabilties to be enabled.

</br>

### Optimistic concurrency

Where the likes of entity versioning and/or optimistic concurrency is required then the [`IETag`](./IEtag.cs) interface provides the requisite [`ETag`](https://en.wikipedia.org/wiki/HTTP_ETag). This is the preferred approach for the likes of RESTful APIs that support [`If-Match`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match). 

Additionally, see how the [WebApis](../WebApis) namespace leverages; including the corresponding [`ETagGenerator`](../Abstractions/ETagGenerator.cs).

</br>

### Multi-tenancy

Within a multi-tenanted implementation the [`ITenantId`](./ITenantId.cs) interface can enable the underlying `TenantId` where it needs to be attributed to an entity (or most likely the underlying data model).

</br>

### Logical delete

Within an implementation the [`ILogicallyDeleted`](./ILogicallyDeleted.cs) interface can enable logical versus physical delete (`IsDeleted`) to be attributed to an entity (or most likely the underlying data model).

<br/>

## Extended capabilities

The `CoreEx.Entities.Extended` namespace contains extended, more advanced, capabiltiies as follows.

<br/>

### Entity base

To support more advanced entity capabilities there are two key base classes depending on the desired functionality required, the latter inherits from the former.

Class | Description
-|-
[`EntityCore`](./Extended/EntityCore.cs) | Provides the core [`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged), [`IChangeTracking`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.ichangetracking), and [`IReadOnly`](./IReadonly.cs) support. The `SetValue` method must be used for all property value updates to enable.
[`EntityBase`](./Extended/EntityBase.cs) | Extends the above `EntityCore`, adding [`ICleanUp`](./ICleanUp.cs), [`IInitial`](./IInitial.cs) and [`ICopyFrom`](./ICopyFrom.cs) support. To function correctly, the `GetPropertyValues` method must be overridden with each property yielded as a [`PropertyValue`](./Extended/PropertyValue.cs) to enable. See [`ChangeLogEx`](./Extended/ChangeLogEx.cs) as an example implementation. There is an `EntityBase` extension method to support deep `Clone` operations (note that the `ICloneable` interface is _not_ explicitly supported by default).

There is a corresponding [`EntityBaseCollection`](./Extended/EntityBaseCollection.cs) and [`EntityCollectionResult`](./Extended/EntityCollectionResult.cs) to support an `EntityBase` collection and paging result equivalence.

Additionally, there is an [`EntityBaseDictionary`](./Extended/EntityBaseDictionary.cs) that inherits from the [`ObservableDictionary`](./Extended/ObservableDictionary.cs) where an `IDictionary` implementation is required.

<br/>

### Extended change audit

The [`ChangeLogEx`](./Extended/ChangeLogEx.cs) is an extended (inherits `EntityBase`) implementation of the [`ChangeLog`](./ChangeLog.cs) that overs equivalent audit properties; however, supports all extended features where applicable. 