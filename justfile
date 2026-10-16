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

fmt:
    dotnet tool run fantomas .

check:
    dotnet build -c Release
    dotnet test
    dotnet tool run fantomas --check .
