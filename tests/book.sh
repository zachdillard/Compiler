#!/bin/sh
set -eu

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

dotnet build src/Compiler.csproj \
  --configuration "$configuration" \
  -m:1 \
  -p:UseSharedCompilation=false

exec python3 \
  "$book_tests_dir/test_compiler" \
  "$PWD/$output_dir/Compiler" \
  "$@"
