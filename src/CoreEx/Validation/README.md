# CoreEx.Validation

The `CoreEx.Validation` namespace provides extended validation capabilities.

<br/>

## Motivation

To support a generic, implementation agnostic, means to support validation that can be leveraged (integrated) in a consistent manner within _CoreEx_. Whereby enabling a developer to leverage their respective framework of choice; for example [FluentValidation](#FluentValidation-implementation).

<br/>

## Implementation agnostic

The [`IValidator`](./IValidator.cs) and [`IValidator<T>`](./IValidatorT.cs) interfaces provide the standard implementation agnostic `ValidateAsync` operations that can be, and are, leveraged within _CoreEx_ to provide validation functionality.

A corresponding [`IValidationResult`](./IValidationResult.cs) and [`IValidationResult<T>`](./IValidationResultT.cs) provide the corresponding validation result, that includes `HasErrors` and `Messages` (see [`MessageItemCollection`](../Entities/MessageItemCollection.cs)), and functionality to throw a resulting[`ValidationException`](../ValidationException.cs). 

<br/>

## CoreEx.Validation implementation 

The [`CoreEx.Validation`](../../CoreEx.Validation) project (assembly) provides a _CoreEx_-based implementation of the `IValidator`; being the [`ValidatorBase`](../../CoreEx.Validation/ValidatorBase.cs) and related [`Validator<TEntity>`](../../CoreEx.Validation/ValidatorT.cs), plus utility [`Validator`](../../CoreEx.Validation/Validator.cs).

<br/>

## FluentValidation implementation

[FluentValidation](https://github.com/FluentValidation/FluentValidation) is a popular .NET validator; as such [CoreEx.FluentValidation](../../CoreEx.FluentValidation) is provided to implement, the underlying [ValidatorWrapper](../../CoreEx.FluentValidation/ValidatorWrapper.cs) enables.
