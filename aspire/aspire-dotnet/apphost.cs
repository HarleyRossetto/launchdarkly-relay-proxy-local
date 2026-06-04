#:package Aspire.Hosting.Docker@13.5.0-preview.1.26303.11
#:package Aspire.Hosting.Redis@13.5.0-preview.1.26303.11
#:sdk Aspire.AppHost.Sdk@13.5.0-preview.1.26303.11

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("docker-compose");
compose.WithDashboard(false);
// compose.WithDashboard(cfg => cfg.WithHostPort(18888));

// LaunchDarkly Configuration Parameters
var launchdarklySdkKeyParameter = builder.AddParameter(name: "launchdarkly-sdk-key", secret: true)
    .WithDescription("The SDK key for the LaunchDarkly environment. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys)", enableMarkdown: true);
var launchdarklyEnvironmentIdParameter = builder.AddParameter(name: "launchdarkly-client-side-id")
    .WithDescription("The client-side ID for the LaunchDarkly environment. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys)", enableMarkdown: true);
var launchdarklyEnvironmentNameParameter = builder.AddParameter(name: "launchdarkly-environment-name", "Development")
    .WithDescription("The name of the LaunchDarkly environment, for display purposes only. i.e. Dev, Test, Staging, Production");

var launchdarklyStreamUriParameter = builder.AddParameter(name: "launchdarkly-stream-uri", "https://stream.launchdarkly.com")
    .WithDescription("The URI for the LaunchDarkly streaming endpoint.")
    .WithHidden();
var launchdarklyEventsUriParameter = builder.AddParameter(name: "launchdarkly-events-uri", "https://events.launchdarkly.com")
    .WithDescription("The URI for the LaunchDarkly events endpoint.")
    .WithHidden();

// LaunchDarkly External Service
var externalLaunchDarklyStream = builder.AddExternalService("launchdarkly-stream", launchdarklyStreamUriParameter).ExcludeFromManifest();
var externalLaunchDarklyEvents = builder.AddExternalService("launchdarkly-events", launchdarklyEventsUriParameter).ExcludeFromManifest();

// Redis as a Persistent Data store
var redis = builder.AddRedis("redis", port: 6379)
    .WithDataVolume()
    .WithPersistence();

// https://github.com/launchdarkly/ld-relay/blob/v9/docs/configuration.md
var relayProxy = builder.AddContainer("ld-relay", "launchdarkly/ld-relay:9.0.0-rc.3-static-debian12-nonroot-amd64")
    .WithReference(redis)
    .WithReference(externalLaunchDarklyStream)
    .WithReference(externalLaunchDarklyEvents)
    .WithEnvironment("PORT", "8030")
    .WithEnvironment("USE_REDIS", "true")
    .WithEnvironment(ctx =>
    {
        ctx.EnvironmentVariables["REDIS_TLS"] = ctx.ExecutionContext.IsPublishMode ? "false" : "true";

        // Enable OTLP in non-publish modes to allow telemetry to flow back to Aspire Dashboard
        if (!ctx.ExecutionContext.IsPublishMode)
        {
            ctx.EnvironmentVariables["USE_OTLP"] = "true";
        }
    })
    .WithEnvironment("STREAM_URI", externalLaunchDarklyStream)
    .WithEnvironment("EVENTS_URI", externalLaunchDarklyEvents)
    .WithEnvironment("USE_EVENTS", "true")
    .WithEnvironment("LOG_LEVEL", "info")
    .WithEnvironment($"LD_ENV_{await launchdarklyEnvironmentNameParameter.Resource.GetValueAsync(default)}", launchdarklySdkKeyParameter)
    .WithEnvironment($"LD_CLIENT_SIDE_ID_{await launchdarklyEnvironmentNameParameter.Resource.GetValueAsync(default)}", launchdarklyEnvironmentIdParameter)
    .WithEnvironment($"LD_PREFIX_{await launchdarklyEnvironmentNameParameter.Resource.GetValueAsync(default)}", launchdarklyEnvironmentIdParameter)
    .WithOtlpExporter()
    .WithEndpoint(endpointName: "http", callback: static endpoint =>
    {
        endpoint.Transport = "http";
        endpoint.UriScheme = "http";
        endpoint.TargetPort = 8030;
        endpoint.Port = 8030;
    });

builder.Build().Run();
