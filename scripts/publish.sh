#!/bin/sh

# Stop if a command fails (-e) or an unset variable is used (-u).
set -eu

# Native AOT requires the target OS's toolchain, so publish for the host.
case "$(uname -s)/$(uname -m)" in
  Darwin/arm64) runtime=osx-arm64 ;;
  Linux/x86_64) runtime=linux-x64 ;;
  *)
    echo "Publishing requires an Apple Silicon Mac or Linux x86-64 with the Native AOT toolchain." >&2
    exit 1
    ;;
esac

# Find the repository root relative to this script, regardless of where it runs.
# Clear CDPATH so shell directory-search settings do not affect the result.
script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."

version=$(dotnet msbuild src/Compiler.csproj -getProperty:Version)
publish_dir="artifacts/publish/$runtime/Release"
package_name="compiler-$version-$runtime"
package_dir="artifacts/release/$package_name"

# Publish an optimized Release build for the host. Compiler.csproj enables
# Native AOT, which compiles ahead of time, so the executable runs without an
# installed .NET runtime.
# Exclude debug symbols so the publish folder contains a single executable.
#
# Use one MSBuild worker and disable build servers to avoid sandbox socket errors.
dotnet publish src/Compiler.csproj \
  --configuration Release \
  --runtime "$runtime" \
  --output "$publish_dir" \
  -p:CopyOutputSymbolsToPublishDirectory=false \
  -m:1 \
  --disable-build-servers

# Package the executable and documentation inside one versioned folder.
mkdir -p "$package_dir"
cp "$publish_dir/Compiler" README.md LICENSE "$package_dir/"
# Omit macOS metadata files (._*) from the archive.
COPYFILE_DISABLE=1 tar -czf "artifacts/release/$package_name.tar.gz" \
  -C artifacts/release "$package_name"

echo "Created artifacts/release/$package_name.tar.gz"
