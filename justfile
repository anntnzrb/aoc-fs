default:
    @just --list

run year day *variant:
    dotnet run --project app/Aoc.Cli -c Release -- {{year}} {{day}} {{variant}} < inputs/{{year}}/day$(printf %02d {{day}}).txt

test:
    dotnet test

fetch year day *flags:
    dotnet run --project app/Aoc.Ops -- fetch {{year}} {{day}} {{flags}}

puzzle year day:
    dotnet run --project app/Aoc.Ops -- puzzle {{year}} {{day}}

submit year day part answer:
    dotnet run --project app/Aoc.Ops -- submit {{year}} {{day}} {{part}} '{{answer}}'

new year day:
    dotnet run --project app/Aoc.Ops -- new {{year}} {{day}}

bench year day:
    dotnet run --project app/Aoc.Ops -- bench {{year}} {{day}}

record year day:
    dotnet run --project app/Aoc.Ops -- record {{year}} {{day}}

fmt:
    dotnet tool run fantomas .

# The gate: build once, run the read-only checks in parallel, then test.
check:
    dotnet build -c Release
    just static
    dotnet test

[parallel]
static: analyze lint doc-built format

format:
    dotnet tool run fantomas --check .
    nixfmt --check flake.nix

# The doc check against the Release build `check` just made.
doc-built:
    dotnet run --no-build --project app/Aoc.Ops -c Release -- doc

doc:
    dotnet run --project app/Aoc.Ops -c Release -- doc

lint:
    dotnet fsharplint lint --lint-config fsharplint.json aoc-fs.slnx

# Run every analyzer in the pinned repository and Ionide assemblies.
# Generated obj sources belong to the SDK/test adapter; their style diagnostics
# cannot be fixed here and regeneration would discard edits.
analyze:
    dotnet fsharp-analyzers --project analyzers/Aoc.Analyzers/Aoc.Analyzers.fsproj src/Aoc/Aoc.fsproj app/Aoc.Cli/Aoc.Cli.fsproj app/Aoc.Ops/Aoc.Ops.fsproj tests/Aoc.Tests/Aoc.Tests.fsproj tests/Aoc.Analyzers.Tests/Aoc.Analyzers.Tests.fsproj --analyzers-path analyzers/Aoc.Analyzers/bin/Release/net10.0 --exclude-files '**/obj/**' --configuration Release --fail-on-type-errors --treat-as-error '*'
