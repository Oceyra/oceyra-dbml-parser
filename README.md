# Oceyra Generator Test Helper
The goal of this project is to have a permanent route, for ESP devices to fetch a controlled version of their firmware. This way, you can update a single device, or all at once

[![Build status](https://gitea.duchaine.freeddns.org/ManufacturingTyde/oceyra-core-generator-tests-helper/actions/workflows/publish.yaml/badge.svg?branch=main&event=push)](https://gitea.duchaine.freeddns.org/ManufacturingTyde/oceyra-core-generator-tests-helper/actions/workflows/publish.yaml?query=branch%3Amain+event%3Apush)

## Usage Sample
```c#
var result = SourceGeneratorVerifier.CompileAndTest<ConstructorGenerator>(source);

result
    .ShouldHaveNoErrors()
    .ShouldExecuteWithin(TimeSpan.FromMilliseconds(1000))
    .ShouldHaveGeneratorTimeWithin<ConstructorGenerator>(TimeSpan.FromMilliseconds(100))
    .ShouldGenerateFiles(1);
```
