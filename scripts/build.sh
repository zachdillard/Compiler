#!/bin/sh
set -eu

configuration=${1:-Debug}
if [ "$#" -gt 1 ]; then
  echo "Usage: $0 [Debug|Release]" >&2
  exit 1
fi
case "$configuration" in
  Debug|Release) ;;
  *)
    echo "Usage: $0 [Debug|Release]" >&2
    exit 1
    ;;
esac

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."

exec dotnet build --configuration "$configuration" -m:1 --disable-build-servers
