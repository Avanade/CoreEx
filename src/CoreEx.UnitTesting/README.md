# CoreEx.UnitTesting

The `CoreEx.UnitTesting` namespace extends [_UnitTestEx_](https://github.com/Avanade/UnitTestEx) to enable _CoreEx_ related unit testing capabilities. 

<br/>

## Motivation

To improce and simplify the unit testing of _CoreEx_ related code.

<br/>

## Agent-initiated ASP.NET testing

To test ASP.NET Core Controllers using the _Agent_ pattern, being the usage of a [`TypedHttpClientBase`](../../CoreEx/Http/TypedHttpClientBase.cs) to invoke via an `HttpRequestMessage`, the following enable:
- [`AgentTester<TAgent>`](./AspNetCore/AgentTester.cs) - an _Agent_-based tester that expects no response value.
- [`AgentTester<TAgent, TValue>`](./AspNetCore/AgentTesterT.cs) - an _Agent_-based tester that expects the specified responce value.

<br/>

## Validation testing

To test an [`IValidator`](../CoreEx/Validation/IValidator.cs) `Validation().With()` extension methods are provided, extending the [`GenericTesterBase`](https://github.com/Avanade/UnitTestEx/blob/main/src/UnitTestEx/Generic/GenericTesterBase.cs). The following is an example:

``` csharp
GenericTester.Create<Startup>()
    .ExpectErrors(
        "Name is required.",
        "Price must be between 0 and 100.")
    .Validation().With<ProductValidator, Product>(new Product { Price = 450.95m })
```
