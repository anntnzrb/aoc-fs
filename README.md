# Advent of Code in F#

Run commands from the repository root in the locked Nix development shell (or
direnv with the tracked `.envrc`):

```sh
nix develop
dotnet tool restore
just check
```

Fixtures are ignored and written from statements, so a fresh clone runs only the
tracked unit and analyzer tests.

### Run a day

Inputs are ignored, so fetch before running:

```sh
cp .env.example .env               # Set AOC_SESSION in .env
just fetch <YEAR> <DAY>
just run <YEAR> <DAY>
```

`just run YEAR DAY [EXPLORATION]` reads `inputs/<YEAR>/day<DD>.txt` and runs the
Release CLI. Each answer occupies one line, part 1 first. Run `just fmt` to
format F# files, or `just` to list commands.

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
  <Concept>.fs               Shared modules extracted on demand (Parse.fs ...)
  Y<YEAR>/
    Day<DD>/                 Input.fs, Primary.fs and explorations
    Registry.fs              Year registry: (day, variant) -> solver
  Registry.fs                Combined year registries
app/Aoc.Cli/                 Stdin-filter executable: aoc
app/Aoc.Ops/                 Workflow executable: aoc-ops
analyzers/Aoc.Analyzers/     Repository discipline analyzer (AOC001-AOC007)
templates/                   Primary, exploration, test and registry templates
tests/Aoc.Tests/             Expecto suite on Microsoft.Testing.Platform
  Repository.fs              Repository root discovery
  Fixtures.fs                Fixture discovery and answer comparisons
  CalendarTests.fs           Puzzle date boundaries
  SolutionTests.fs           Answer rendering and registry key invariants
  OpsTests.fs                Offline operation and regeneration checks
tests/Aoc.Analyzers.Tests/   Discipline analyzer tests
bench/                       <YEAR>.csv canonical records, hosts/ snapshots
docs/journal/                Per-year learning log
```

## Workflow operations

Provision `.env` locally with `AOC_SESSION=...` before using network operations.
The user supplies the session; never commit this ignored file. The executable
reads it only when a network request is necessary, never for scaffolding or a
cached fetch.

```sh
just fetch <YEAR> <DAY>            # Reuse cached input
just fetch <YEAR> <DAY> --force    # Replace cached input
just puzzle <YEAR> <DAY>           # Refresh plain puzzle articles, strip past answers
just submit <YEAR> <DAY> 1 ANSWER  # Submit a validated answer token
just new <YEAR> <DAY> [EXPLORATION] # Scaffold a solution, register and format it
just bench <YEAR> <DAY>            # Measure all variants against the primary
just record <YEAR> <DAY>           # Append canonical benchmark row to bench/<YEAR>.csv
just doc                           # Verify solution header tags and #nowarn ban
just lint                          # Run FSharpLint over the solution
just analyze                       # Run repository discipline analyzers
```

Run these through `nix develop -c just ...` or inside `nix develop`. Direct usage
is `dotnet run --project app/Aoc.Ops -- COMMAND ...`. `new` refuses existing day,
test or colliding exploration paths. Network and cache writes replace files
atomically. Submission exits 0 for right, 1 for wrong, 2 for cooldown or already
complete, 3 for an unknown or unrecognised response, and 4 for local, HTTP and
transport failures (including 429 rate limits with retry delays).

## Inputs and fixtures

Place personal input in `inputs/<YEAR>/day<DD>.txt`. Inputs and fixtures are
ignored by Git.

Example fixtures live in `fixtures/<YEAR>/<DD>/`: `p1-*.in` with one-line
`p1-*.out`, and `p2-*.in` with two-line `p2-*.out`. Each expectation must end
with a newline. The runner checks every registered implementation against each
fixture, comparing only the expected number of answer lines. Missing fixture
directories contribute no tests; a missing output file fails its test.

## Adding a year

Running `just new <YEAR> 1` scaffolds the first day, creates the year directory,
and regenerates the year registry, root registry and project compile entries.
Create `docs/journal/<YEAR>.md` by hand to log daily progress and learnings.
Running `just record <YEAR> <DAY>` creates `bench/<YEAR>.csv` on its first run.
Calendar years from 2025 on have 12 puzzle days (2015 through 2024 have 25).
See [AGENTS.md](AGENTS.md) for the full orchestration and integration workflow.

## License

GNU Affero General Public License v3.0, see [LICENSE](LICENSE).
