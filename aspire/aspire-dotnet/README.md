# .NET Aspire AppHost

This Aspire AppHost includes the Relay Proxy and Redis containers.

__What is Aspire?__
> Aspire is a code-first orchestration and observability layer for distributed applications.

> Use Aspire to model the services, frontends, databases, queues, caches, containers, and cloud dependencies that make up your app. In production they work together as one system, but during development they often need to be started separately, configured by hand, and debugged across multiple terminals and dashboards. Aspire gives you one place to define that system, one command to run it, and one toolchain to observe it.

You can add your own applications, containers, and dependencies to this AppHost to build out your local development environment.

To learn more about Aspire, what it is, and how to use it, check out the [Aspire documentation](https://aspire.dev).

Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/)

Then run the following command:

```bash
aspire run 
```

The Relay Proxy will be available at `http://localhost:8030` and the Redis instance will be available at `localhost:6379`.

The .NET AppHost is also responsible for publishing the [Docker Compose](../../docker/) artifacts, which can be used to run the Relay Proxy and Redis containers without Aspire. To publish the Docker Compose artifacts, run the following command from this directory:

```bash
aspire publish -o ../../docker
```
