# pack into ~/.nuget/local-feed as a prerelease of the current version
nuget-local:
    #!/usr/bin/env bash
    set -euo pipefail
    version=$(dotnet msbuild UnitySceneRepacker.csproj -getProperty:Version)
    dotnet pack -c Release -o ~/.nuget/local-feed -p:Version="$version-dev.$(date +%Y%m%d%H%M%S)"
