#!/bin/sh

# Stop if a command fails (-e) or an unset variable is used (-u).
set -eu

# Publish only on Apple Silicon. Native AOT requires the target OS's toolchain.
if [ "$(uname -s)/$(uname -m)" != "Darwin/arm64" ]; then
  echo "Publishing requires an Apple Silicon Mac with Xcode Command Line Tools." >&2
  exit 1
fi

# Find the repository root relative to this script, regardless of where it runs.
# Clear CDPATH so shell directory-search settings do not affect the result.
script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."

# Publish an optimized Release build for Apple Silicon. Native AOT compiles
# ahead of time, so the executable runs without an installed .NET runtime.
# Exclude debug symbols so the publish folder contains a single executable.
#
# Use one MSBuild worker and disable build servers to avoid sandbox socket errors.
# exec replaces this shell with dotnet, preserving dotnet's exit code.
exec dotnet publish src/Compiler.csproj \
  --configuration Release \
  --runtime osx-arm64 \
  --output artifacts/publish/osx-arm64/Release \
  -p:PublishAot=true \
  -p:CopyOutputSymbolsToPublishDirectory=false \
  -m:1 \
  --disable-build-servers
