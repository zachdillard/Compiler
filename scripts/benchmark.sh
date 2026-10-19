#!/bin/sh
set -eu

mode=${1:-all}
case "$mode" in
  all|micro|cli) ;;
  *) echo "Usage: $0 [all|micro|cli]" >&2; exit 1 ;;
esac
if [ "$#" -gt 1 ]; then
  echo "Usage: $0 [all|micro|cli]" >&2
  exit 1
fi

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir/.."
case "$(uname -s)/$(uname -m)" in
  Darwin/arm64) runtime=osx-arm64 ;;
  Linux/x86_64) runtime=linux-x64 ;;
  *) echo "Benchmarks require an Apple Silicon Mac or Linux x86-64." >&2; exit 1 ;;
esac

command -v dotnet >/dev/null
command -v gcc >/dev/null
if [ "$mode" != micro ]; then
  command -v hyperfine >/dev/null
fi

mkdir -p artifacts/benchmarks
results=$(mktemp -d "$PWD/artifacts/benchmarks/$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")
echo "Reports: $results"
{
  date -u
  git rev-parse HEAD
  git status --short
  dotnet --info
  gcc --version
  uname -a
  if [ "$(uname -s)" = Darwin ]; then
    sysctl -n machdep.cpu.brand_string
  else
    lscpu
  fi
  if [ "$mode" != micro ]; then hyperfine --version; fi
  echo "Mode: $mode"
  echo "Micro job: ${BENCHMARK_JOB:-Short}"
  echo "CLI warmups: ${BENCHMARK_WARMUPS:-3}"
  echo "CLI runs: ${BENCHMARK_RUNS:-20}"
} > "$results/environment.txt" 2>&1

# Record expanded commands separately from measurement reports.
exec 3> "$results/commands.txt"
run() {
  for argument do printf "[%s] " "$argument" >&3; done
  printf '\n' >&3
  "$@"
}

if [ "$mode" != cli ]; then
  if ! run dotnet run --project benchmarks/Compiler.Benchmarks.csproj -c Release \
    -- --filter '*' --job "${BENCHMARK_JOB:-Short}" \
    --artifacts "$results/micro" --exporters json github \
    > "$results/micro.log" 2>&1; then
    tail -60 "$results/micro.log" >&2
    exit 1
  fi
fi

if [ "$mode" != micro ]; then
  # Build outside Hyperfine's measured commands.
  if ! run dotnet publish src/Compiler.csproj -c Release -r "$runtime" \
    -o "$results/publish" -p:CopyOutputSymbolsToPublishDirectory=false \
    -m:1 --disable-build-servers > "$results/publish.log" 2>&1; then
    tail -60 "$results/publish.log" >&2
    exit 1
  fi

  temporary=$(mktemp -d)
  trap 'rm -rf "$temporary"' EXIT
  printf 'int main(void) { return 2; }\n' > "$temporary/input.c"
  # Fixed relative paths avoid shell quoting differences and isolate outputs.
  cp "$results/publish/Compiler" "$temporary/Compiler"
  cd "$temporary"
  run hyperfine --warmup "${BENCHMARK_WARMUPS:-3}" --runs "${BENCHMARK_RUNS:-20}" \
    --export-json "$results/cli.json" --export-markdown "$results/cli.md" \
    './Compiler --lex input.c' \
    './Compiler --codegen input.c' \
    './Compiler -S input.c' \
    './Compiler input.c' > "$results/cli.log" 2>&1 || {
      tail -60 "$results/cli.log" >&2
      exit 1
    }
fi

echo "Benchmarks completed: $results"
