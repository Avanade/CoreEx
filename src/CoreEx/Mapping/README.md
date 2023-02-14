# CoreEx.Mapping

The `CoreEx.Mapping` namespace provides extended mapping capabilities.

<br/>

## Motivation

To support a generic, implementation agnostic, means to support object property mappings that can be leveraged (integrated) in a consistent manner within _CoreEx_. Whereby enabling a developer to leverage their respective framework of choice; for example [AutoMapper](#AutoMapper).

<br/>

## Implementation agnostic

The [`IMapper`](./IMapper.cs) interface provides the standard implementation agnostic `Map` operations that can be, and are, leveraged within _CoreEx_ to provide object property mapping functionality.

<br/>

## Mapper implementation 

The [`Mapper`](./Mapper.cs) class provides a simple (explicit) mapping implementation of the `IMapper`. It for the most part does _not_ employ reflection and/or run-time compilation; therefore, it is very simple and fast.

The source and destintaion types and corresponding property mappings are configured using the [`Mapper<TSource, TDestination>`](./MapperT.cs) class which are registered with the owning `Mapper` using the `Register` method. Collection mappings are automatically enabled.

Mappings are configured using the following methods.

Method | Description
-|-
`Map` | Adds a value-based mapping, generally property to property, including an [`IConverter`](./Converters/IConverter.cs) where applicable.
`Flatten` | Adds a flatten-mapping of a nested property. Flattening is the updating of the destination from a source property that is a nested class, that in turn contains the actual corresponding source properties. Where the source property is `null` the corresponding destination properties are still updated as a temporary source property instance is instantiated and used.
`Expand` | Adds an expand-mapping to a nested property. Expanding is the reverse of flattening (`Flatten`).
`Base` | Adds a base-mapping; where the source and destination type configuration is inherited from the base.

The [`MapperTest`](../../../tests/CoreEx.Test/Framework/Mapping/MapperTest.cs) demonstrates usage.

<br/>

## Converters

The [`IConverter`](./Converters/IConverter.cs) interface provides a standardized approach to value conversion. A number of [converters](./Converters) are provided for common conversion requirements.

<br/>

## AutoMapper implementation

[AutoMapper](https://github.com/AutoMapper/AutoMapper) is a popular .NET mapper; as such [CoreEx.AutoMapper](../../CoreEx.AutoMapper) is provided to implemenent, the underlying [AutoMapperWrapper](../../CoreEx.AutoMapper/AutoMapperWrapper.cs) enables.


