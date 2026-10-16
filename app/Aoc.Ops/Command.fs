/// Parses operation arguments before session or filesystem access.
module Aoc.Ops.Command

open System
open System.Text.RegularExpressions
open Aoc

/// A validated operation and puzzle date.
type Operation =
    /// Fetch input, optionally replacing the cache.
    | Fetch of year: int * day: int * force: bool
    /// Refresh puzzle articles.
    | Puzzle of year: int * day: int
    /// Submit a validated answer.
    | Submit of year: int * day: int * part: int * answer: string
    /// Measure optimized variants on cached input.
    | Bench of year: int * day: int
    /// Append canonical measurements on clean main.
    | Record of year: int * day: int
    /// Scaffold a primary solution.
    | New of year: int * day: int
    /// Scaffold a named exploration beside an existing primary.
    | NewExploration of year: int * day: int * name: string

/// Accepts ASCII PascalCase module identifiers except the two reserved day modules.
let explorationName (name: string) =
    if
        Regex.IsMatch(name, @"\A[A-Z][A-Za-z0-9]*\z")
        && name <> "Primary"
        && name <> "Input"
    then
        Ok name
    else
        Error "exploration must be a PascalCase identifier other than Primary or Input"

/// Rejects malformed dates, unsupported operations and invalid submissions.
let parse (args: string array) : Result<Operation, string> =
    let date year day operation =
        match Int32.TryParse(year: string), Int32.TryParse(day: string) with
        | (true, year), (true, day) when Calendar.isPuzzleDay year day -> Ok(operation year day)
        | _ -> Error "invalid puzzle year or day"

    match args with
    | [| "fetch"; year; day |] -> date year day (fun y d -> Fetch(y, d, false))
    | [| "fetch"; year; day; "--force" |] -> date year day (fun y d -> Fetch(y, d, true))
    | [| "bench"; year; day |] -> date year day (fun y d -> Bench(y, d))
    | [| "record"; year; day |] -> date year day (fun y d -> Record(y, d))
    | [| "puzzle"; year; day |] -> date year day (fun y d -> Puzzle(y, d))
    | [| "submit"; year; day; part; answer |] ->
        Submission.validate part answer
        |> Result.bind (fun (part, answer) -> date year day (fun y d -> Submit(y, d, part, answer)))
    | [| "new"; year; day; name |] ->
        explorationName name
        |> Result.bind (fun name -> date year day (fun y d -> NewExploration(y, d, name)))
    | [| "new"; year; day |] -> date year day (fun y d -> New(y, d))
    | _ ->
        Error
            "Usage: aoc-ops fetch YEAR DAY [--force] | puzzle YEAR DAY | submit YEAR DAY PART ANSWER | new YEAR DAY [EXPLORATION] | bench YEAR DAY | record YEAR DAY"
