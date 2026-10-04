// Aspire Go AppHost
// For more information, see: https://aspire.dev

package main

import (
	"log"

	"apphost/modules/aspire"
)

func main() {
	builder, err := aspire.CreateBuilder()
	if err != nil {
		log.Fatal(aspire.FormatError(err))
	}

	launchdarklySdkKey := builder.AddParameter("launchdarkly-sdk-key", &aspire.AddParameterOptions{Secret: new(true)}).
		WithDescription(
			"The SDK key for the LaunchDarkly environment to connect to. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys",
			&aspire.WithDescriptionOptions{EnableMarkdown: new(true)})

	launchdarklyEnvironmentID := builder.AddParameter("launchdarkly-environment-id").
		WithDescription("The client-side ID for the LaunchDarkly environment. [SDK Keys](https://app.launchdarkly.com/settings/sdk-keys",
			&aspire.WithDescriptionOptions{EnableMarkdown: new(true)})

	launchdarklyStreamURIParameter := builder.AddParameter("launchdarkly-stream-uri", &aspire.AddParameterOptions{Value: new("https://stream.launchdarkly.com")}).
		WithDescription("The URI for the LaunchDarkly streaming endpoint").
		WithHidden()
	launchdarklyEventsURIParameter := builder.AddParameter("launchdarkly-events-uri", &aspire.AddParameterOptions{Value: new("https://events.launchdarkly.com")}).
		WithDescription("The URI for the LaunchDarkly events endpoint").
		WithHidden()

	externalLaunchDarklyStream := builder.AddExternalService("launchdarkly-stream", launchdarklyStreamURIParameter).
		ExcludeFromManifest()
	externalLaunchDarklyEvents := builder.AddExternalService("launchdarkly-events", launchdarklyEventsURIParameter).
		ExcludeFromManifest()
	if err = externalLaunchDarklyStream.Err(); err != nil {
		log.Fatal(aspire.FormatError(err))
	}
	if err = externalLaunchDarklyEvents.Err(); err != nil {
		log.Fatal(aspire.FormatError(err))
	}

	// Add Redis persistent store
	redis := builder.AddRedis("redis", &aspire.AddRedisOptions{Port: aspire.Float64Ptr(6379)}).
		WithDataVolume().
		WithPersistence()
	if err = redis.Err(); err != nil {
		log.Fatal(aspire.FormatError(err))
	}

	// Add Relay
	relay := builder.AddContainer("ld-relay", "launchdarkly/ld-relay:9.0.0-rc.5-static-debian12-nonroot-amd64").
		WithReference(redis).
		WithReference(externalLaunchDarklyEvents).
		WithReference(externalLaunchDarklyStream).
		WithEnvironment("USE_OTLP", "true").
		WithEnvironment("PORT", "8030").
		WithEnvironment("USE_REDIS", "true").
		WithEnvironment("REDIS_TLS", "true").
		WithEnvironment("LD_PREFIX_LaunchDarkly", launchdarklyEnvironmentID).
		WithEnvironment("STREAM_URI", externalLaunchDarklyStream).
		WithEnvironment("EVENTS_URI", externalLaunchDarklyEvents).
		WithEnvironment("LD_ENV_LaunchDarkly", launchdarklySdkKey).
		WithEnvironment("LD_CLIENT_SIDE_ID_LaunchDarkly", launchdarklyEnvironmentID).
		WithOtlpExporter().
		WithEndpoint(&aspire.WithEndpointOptions{
			Name:       new("http"),
			Scheme:     new("http"),
			TargetPort: aspire.Float64Ptr(8030),
			Port:       aspire.Float64Ptr(8030),
		})

	if err = relay.Err(); err != nil {
		log.Fatal(aspire.FormatError(err))
	}

	app, err := builder.Build()
	if err != nil {
		log.Fatal(aspire.FormatError(err))
	}
	if err := app.Run(); err != nil {
		log.Fatal(aspire.FormatError(err))
	}
}
