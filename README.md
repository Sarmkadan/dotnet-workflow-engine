<full file content here>
## RateLimitingMiddlewareTests
The `RateLimitingMiddlewareTests` class provides a set of tests for the rate limiting middleware. It tests various scenarios such as requests under the limit, requests over the limit, and exempt paths. Here is an example of how to use it:
```csharp
var tests = new RateLimitingMiddlewareTests();
await tests.InvokeAsync_RequestUnderLimit_PassesThrough();
await tests.InvokeAsync_RequestOverLimit_Returns429();
```
