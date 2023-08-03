# Basic Honeycomb Refinery example with docker

This repo provides a simple example of using Honeycomb refinery. The goal is to remove the barriers of Kubernetes and scaling, so that you can experiment with rules and understand how the structure works.

The repository includes:

* `/app` A .NET api project out of the box with OpenTelemetry added, pointing the exporter to `localhost:4317` (the default)
* `/infra` A docker-compose file including the OpenTelemetry collector and refinery with some initial, sane configs.

## Running

First you'll need a Honeycomb account, a free account is sufficient. Once you have this, make note of the name of the environment as you'll need this. The default name for this project is `refinery-test`, if you choose something else, you can change that in this project. If you don't change it, things will still work, but they'll be hitting something called the deterministic sampler instead of your rules.

## Run refinery and the collector

Set your Honeycomb API key as an environment variable

```shell
export HONEYCOMB_API_KEY=<your key>
```

Then bring up the containers

```shell
cd infra
docker-compose up -d
```

## Run the app

```shell
cd app
dotnet run
```

## Changing the Honeycomb environment

If you've set a different honeycomb environment name than `refinery-test`, you will need to change the value in `infra/refinery/rules.yaml`

## Refinery rules in play

The rules refinery uses are in [rules.yaml](infra/refinery/rules.yaml) file.

Here is a description of how the rules in this example are processed. The order here is important.

1. Keep any traces that contain a http 500 error.
  Note that this doesn't mean keeping all spans with an error, it just means keeping http spans with an error code.

1. Keep any traces with a gRPC error code

1. Keep any trace where there is a span with an `error` attribute

1. Keep any spans where the root span exceeds 1700ms
  Note that this is only root spans, not the overall trace duration. If all spans finish in 1000ms, but the overall trace duration is 3000ms, this rule won't be triggered.

1. Dynamically sample all traces where the root span has a 200-400 http status code to keep 1 in 5
  This compares traces using the `service.name`, `http.target`, `http.status_code` and `http.method` attribute from all spans to consider whether they are the same.

1. Everything else is sampled to keep 1 in 25
  This compares traces using `service.name`, `http.status_code` and `status_code` attributes across all the spans to consider whether they are the same.

---
A note on the comparison of similar spans.

The samplers in refinery will search all the spans in a trace and:

1. extract all of the specified attributes as they exist
1. Sort the values of these attribute
1. Concatenate them as a single string

That is the key. There are limitations of this approach resulting in traces that aren't the same being considered the same. Your system is unique, and you will know best how to solve this.

---
