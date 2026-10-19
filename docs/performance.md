# Performance testing

Performance measurements are separate from the correctness tests. Reports are
informational: there are no timing thresholds that block a pull request.

## Run locally

Use the SDK pinned in `global.json`, GCC on `PATH`, and Hyperfine. On macOS,
install Hyperfine with `brew install hyperfine`; on Ubuntu use
`sudo apt-get install hyperfine`. Native AOT publishing also needs the native
toolchain described in the development guide. Supported benchmark hosts are
Apple Silicon macOS and Linux x86-64.

From the repository root:

```sh
./scripts/benchmark.sh        # Both suites
./scripts/benchmark.sh micro  # C# stages only
./scripts/benchmark.sh cli    # Published Native AOT command only
```

Each run gets its own ignored directory under `artifacts/benchmarks/`. It contains
reports, logs, exact argument lists in `commands.txt`, and `environment.txt`
with the commit, working-tree status, SDK, GCC, OS, CPU, and run settings.
Command arguments are recorded between square brackets, not as a shell script.

The default BenchmarkDotNet job is `Short`, intended for development feedback.
For a baseline, use `BENCHMARK_JOB=Default ./scripts/benchmark.sh micro`.
Validate compilation and execution quickly with
`BENCHMARK_JOB=Dry ./scripts/benchmark.sh micro`; dry results are not useful
performance measurements. The suite contains six cases: two methods times three
inputs. For a focused run:

```sh
dotnet run --project benchmarks/Compiler.Benchmarks.csproj -c Release -- \
  --filter '*LexerBenchmarks*' --job Short \
  --artifacts artifacts/benchmarks/focused --exporters json github
```

## What is measured

The BenchmarkDotNet project measures warmed-up **JIT** code, reporting elapsed
time and managed allocations per operation:

- `LexerBenchmarks.Lex`: source string to tokens.
- `PipelineBenchmarks.Compile`: source string through lexer, parser, generator,
  and emitter, returning assembly text. It excludes GCC, file I/O, and process
  startup. The lexer creates fresh tokens for every invocation because the parser
  consumes its list.

Inputs are prepared outside the measured methods. The three cases are a minimal
chapter-one program, a 265-character function identifier, and a program with
4 KiB of whitespace. They are valid single-function programs, not a substitute
for benchmarks of larger programs when the language supports them. Whitespace
is measured directly in the lexer; preprocessing can normalize it in CLI runs.

The benchmark project pins a BenchmarkDotNet prerelease for .NET 11 support.
Validate a dry run when upgrading either the package or SDK. Enabling Native AOT
in the referenced compiler project does not make the default benchmark job AOT.

Hyperfine measures the **published Release Native AOT executable**. Publishing
happens once outside timing. The same minimal `main` fixture is used for `--lex`,
`--codegen`, `-S`, and full compilation. Every command includes fresh process
startup, argument parsing, GCC preprocessing, and temporary-file handling.
Full compilation also includes assembly and linking; `-S` includes writing
assembly. These are cumulative CLI modes, not isolated stage measurements.
Results include three warmups and twenty measured runs per command by default.
Override with `BENCHMARK_WARMUPS` and `BENCHMARK_RUNS`.

CLI inputs, binary, and outputs live in a temporary directory removed on exit;
the published binary remains with the reports. Compiler output is not executed,
so these timings do not measure generated-program performance. Hyperfine reports
elapsed time, not allocations or peak memory for the process tree.

## CI and comparisons

Run the **Benchmarks** workflow manually from GitHub Actions, selecting the
branch to measure. It uses Ubuntu 24.04 and uploads reports even if a later step
fails. Benchmark failures fail the workflow; timing differences do not.

Keep JIT and Native AOT baselines separate. Compare identical inputs, jobs, SDKs,
GCC versions, and CPU architectures. For before/after comparisons, run base and
candidate sequentially on the same idle machine and retain both report folders.
Separate GitHub-hosted runs are useful for trends but can run on different CPUs.
Repeat apparent regressions before drawing conclusions; establish normal
variation before introducing a percentage-based gate.

Future additions can isolate generation and emission, or measure parsing plus
token-list copying with the copying cost explicitly included in the name.
