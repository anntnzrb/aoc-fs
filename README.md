# Advent of Code in F#

Run commands from the repository root in the locked Nix development shell:

```sh
nix develop
dotnet tool restore
just run 2017 1
just test
just check
```

`just run YEAR DAY [EXPLORATION]` reads `inputs/<year>/dayDD.txt` and runs the
Release CLI. Each answer occupies one line, part 1 first. No solvers are registered
yet. Run `just fmt` to format F# files, or `just` to list commands.

To supply stdin directly:

```sh
dotnet run --project app/Aoc.Cli -- YEAR DAY < file
```

Valid arguments with empty stdin produce no output and exit successfully.
Non-empty input requires a registered solver.

## Layout

```text
src/Aoc/                     Pure solution library
  Calendar.fs                Puzzle day ranges
  Solution.fs                Answers, solver and registry key contracts
  Y2017/
    Day01/                   Future Input.fs, Primary.fs and explorations
    Registry.fs              Year registry (currently empty)
  Registry.fs                Combined year registries
app/Aoc.Cli/                 Stdin-filter executable: aoc
app/Aoc.Ops/                 Workflow executable: aoc-ops
templates/                   Primary scaffold and registry templates
tests/Aoc.Tests/              Expecto suite on Microsoft.Testing.Platform
  Repository.fs              Repository root discovery
  Fixtures.fs                Fixture discovery and answer comparisons
  CalendarTests.fs           Puzzle date boundaries
  SolutionTests.fs           Answer rendering and registry key invariants
  OpsTests.fs                Offline operation and regeneration checks
```

## Workflow operations

Provision `.env` locally with `AOC_SESSION=...` before using network operations.
The user supplies the session; never commit this ignored file. The executable
reads it only when a network request is necessary, never for scaffolding or a
cached fetch.

```sh
just fetch 2017 1            # Reuse cached input
just fetch 2017 1 --force    # Replace cached input
just puzzle 2017 1           # Refresh plain puzzle articles, strip past answers
just submit 2017 1 1 ANSWER  # Submit a validated answer token
just new 2017 1              # Scaffold a primary, register and format it
```

Run these through `nix develop -c just ...` or inside `nix develop`. Direct usage
is `dotnet run --project app/Aoc.Ops -- COMMAND ...`. `new` refuses existing day
or test paths; exploration scaffolds are not supported yet. Network and cache
writes replace files atomically. Submission exits 0 for right, 1 for wrong,
2 for cooldown/already complete, and 3 for an unknown verdict. All local errors
(including usage, validation and HTTP failures) exit 4.

## Inputs and fixtures

Place personal input in `inputs/<year>/dayDD.txt`, for example
`inputs/2017/day01.txt`. Inputs and fixtures are ignored by Git.

Example fixtures live in `fixtures/<year>/<DD>/`: `p1-*.in` with one-line
`p1-*.out`, and `p2-*.in` with two-line `p2-*.out`. Each expectation must end
with a newline. The runner checks every registered implementation against each
fixture, comparing only the expected number of answer lines. Missing fixture
directories contribute no tests; a missing output file fails its test.

See [AGENTS.md](AGENTS.md) for architecture, coding rules and the workflow for
adding solutions, explorations and years.
