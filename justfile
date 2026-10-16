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

check:
    dotnet build -c Release
    just analyze
    just lint
    just doc
    dotnet test
    dotnet tool run fantomas --check .

doc:
    dotnet run --project app/Aoc.Ops -c Release -- doc

lint:
    dotnet fsharplint lint --lint-config fsharplint.json aoc-fs.slnx

analyze:
    dotnet fsharp-analyzers --project analyzers/Aoc.Analyzers/Aoc.Analyzers.fsproj src/Aoc/Aoc.fsproj app/Aoc.Cli/Aoc.Cli.fsproj app/Aoc.Ops/Aoc.Ops.fsproj tests/Aoc.Tests/Aoc.Tests.fsproj tests/Aoc.Analyzers.Tests/Aoc.Analyzers.Tests.fsproj --analyzers-path analyzers/Aoc.Analyzers/bin/Release/net10.0 --include-analyzers Aoc.Discipline --configuration Release --fail-on-type-errors --treat-as-error '*'
