# Resources for local development feature ld-relay

This repository containers resources for local development of [LaunchDarkly](launchdarkly.com)'s [Relay Proxy](https://github.com/launchdarkly/ld-relay) application.

The current resources focus on running an ld-relay instance for flag delivery and a [Redis](redis.io) instance as a [Persistent store](https://github.com/launchdarkly/ld-relay/blob/v9/docs/persistent-storage.md).

This includes:
- A C# .NET [Aspire.dev](aspire.dev) AppHost
- A TypeScript [Aspire.dev](aspire.dev) AppHost
- A Docker Compose (published via the .NET AppHost)
