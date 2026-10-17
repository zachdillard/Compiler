#!/bin/sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."

dotnet test --configuration "${BUILD_CONFIGURATION:-Debug}" -m:1 --disable-build-servers

if [ "$#" -eq 0 ]; then
  set -- --chapter 1
fi

exec "$script_dir/book.sh" --no-build "$@"
