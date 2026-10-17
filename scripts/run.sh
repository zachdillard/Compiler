#!/bin/sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)

exec dotnet run --project "$script_dir/../src/Compiler.csproj" -- "$@"
