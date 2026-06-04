// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

// LaunchDarkly Configuration Parameters
var launchdarklySdkKeyParameter = await builder.addParameter("launchdarkly-sdk-key", { secret: true })
    .withDescription("The SDK key for the LaunchDarkly environment. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys)", { enableMarkdown: true });
var launchdarklyEnvironmentIdParameter = await builder.addParameter("launchdarkly-client-side-id")
    .withDescription("The client-side ID for the LaunchDarkly environment. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys)", { enableMarkdown: true });
var launchdarklyEnvironmentNameParameter = await builder.addParameter("launchdarkly-environment-name")
    .withDescription("The name of the LaunchDarkly environment, for display purposes only. i.e. Dev, Test, Staging, Production")
    .withHidden();

var launchdarklyStreamUriParameter = await builder.addParameter("launchdarkly-stream-uri", { value: "https://stream.launchdarkly.com" })
    .withDescription("The URI for the LaunchDarkly streaming endpoint.")
    .withHidden();
var launchdarklyEventsUriParameter = await builder.addParameter("launchdarkly-events-uri", { value: "https://events.launchdarkly.com" })
    .withDescription("The URI for the LaunchDarkly events endpoint.")
    .withHidden();

// LaunchDarkly External Service
var externalLaunchDarklyStream = await builder.addExternalService("launchdarkly-stream", launchdarklyStreamUriParameter);
var externalLaunchDarklyEvents = await builder.addExternalService("launchdarkly-events", launchdarklyEventsUriParameter);

// Redis as a Persistent Data store
var redis = await builder.addRedis("redis", { port: 6379 })
    .withDataVolume()
    .withPersistence();

var relayProxy = await builder.addContainer("ld-relay", { image: "launchdarkly/ld-relay:9.0.0-rc.3-static-debian12-nonroot-amd64" })
    .withReference(redis)
    .withReference(externalLaunchDarklyStream)
    .withReference(externalLaunchDarklyEvents)
    .withEnvironment("PORT", "8030")
    .withEnvironment("USE_REDIS", "true")
    .withEnvironment("REDIS_TLS", "true")
    .withEnvironment("STREAM_URI", externalLaunchDarklyStream)
    .withEnvironment("EVENTS_URI", externalLaunchDarklyEvents)
    .withEnvironment("USE_EVENTS", "true")
    .withEnvironment("LOG_LEVEL", "info")
    .withEnvironment("USE_OTLP", "true")
    .withEnvironment("LD_ENV_Local", launchdarklySdkKeyParameter)
    .withEnvironment("LD_CLIENT_SIDE_ID_Local", launchdarklyEnvironmentIdParameter)
    .withEnvironment("LD_PREFIX_Local", launchdarklyEnvironmentIdParameter)
    .withOtlpExporter()
    .withEndpoint({
        name: "http",
        port: 8030,
        targetPort: 8030,
        scheme: "http",
    })

await builder.build().run();
