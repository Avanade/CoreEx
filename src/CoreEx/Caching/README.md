# CoreEx.Caching

The `CoreEx.Caching` namespace provides additional caching capabilities.

<br/>

## Motivation

To provide additional capabilities to cache data to improve runtime performance.

<br/>

## Request cache

The [`IRequestCache`](./IRequestCache.cs) interface and corresponding [`RequestCache`](./RequestCache.cs) implementation are intended to provide generic short-lived request caching; for example, to reduce data chattiness within the context of a request scope.