#!/bin/sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."

if [ -n "${BOOK_TESTS_DIR:-}" ]; then
  book_tests_dir="$BOOK_TESTS_DIR"
else
  book_tests_dir=../writing-a-c-compiler-tests
fi

if [ -n "${BUILD_CONFIGURATION:-}" ]; then
  configuration="$BUILD_CONFIGURATION"
else
  configuration=Debug
fi

output_dir="src/bin/$configuration/net11.0"

if [ "${1:-}" = "--no-build" ]; then
  shift
else
  dotnet build src/Compiler.csproj \
    --configuration "$configuration" \
    -m:1 \
    --disable-build-servers \
    -p:UseSharedCompilation=false
fi

exec python3 \
  "$book_tests_dir/test_compiler" \
  "$PWD/$output_dir/Compiler" \
  "$@"
