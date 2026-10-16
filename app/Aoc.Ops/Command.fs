/// Parses operation arguments before session or filesystem access.
module Aoc.Ops.Command

open System
open Aoc

/// A validated operation and puzzle date.
type Operation =
    /// Fetch input, optionally replacing the cache.
    | Fetch of year: int * day: int * force: bool
    /// Refresh puzzle articles.
    | Puzzle of year: int * day: int
    /// Submit a validated answer.
    | Submit of year: int * day: int * part: int * answer: string
    /// Scaffold a primary solution.
    | New of year: int * day: int

/// Rejects malformed dates, unsupported operations and invalid submissions.
let parse (args: string array) : Result<Operation, string> =
    let date year day operation =
        match Int32.TryParse(year: string), Int32.TryParse(day: string) with
        | (true, year), (true, day) when Calendar.isPuzzleDay year day -> Ok(operation year day)
        | _ -> Error "invalid puzzle year or day"

    match args with
    | [| "fetch"; year; day |] -> date year day (fun y d -> Fetch(y, d, false))
    | [| "fetch"; year; day; "--force" |] -> date year day (fun y d -> Fetch(y, d, true))
    | [| "puzzle"; year; day |] -> date year day (fun y d -> Puzzle(y, d))
    | [| "submit"; year; day; part; answer |] ->
        Submission.validate part answer
        |> Result.bind (fun (part, answer) -> date year day (fun y d -> Submit(y, d, part, answer)))
    | [| "new"; _; _; _ |] -> Error "explorations are not supported yet"
    | [| "new"; year; day |] -> date year day (fun y d -> New(y, d))
    | _ ->
        Error "Usage: aoc-ops fetch YEAR DAY [--force] | puzzle YEAR DAY | submit YEAR DAY PART ANSWER | new YEAR DAY"
