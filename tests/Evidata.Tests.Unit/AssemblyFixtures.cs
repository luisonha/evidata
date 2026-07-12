using Xunit;

// Building real ASP.NET Core / Azure Functions Worker hosts (HostDiValidationTests)
// touches process-wide static state (OpenTelemetry SDK defaults, diagnostics
// listeners, Aspire ServiceDefaults registration, etc.). Running those host
// builds concurrently with unrelated test classes caused intermittent,
// non-deterministic DI resolution failures (observed directly: the same test
// passed reliably in isolation but failed ~1 in 4 runs of the full suite).
// Disabling collection-level parallelization for the whole assembly trades a
// small amount of wall-clock time (the full suite still runs in ~1s) for
// deterministic results — a flaky safety-net test is worse than no test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
