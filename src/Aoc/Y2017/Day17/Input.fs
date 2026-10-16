/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day17.Input

/// The non-negative number of circular steps before each insertion.
[<Struct>]
type Steps = Steps of value: int

/// Parses the input into the puzzle domain model.
let parse (input: string) : Result<Steps, string> =
    match
        System.Int32.TryParse(
            input.Trim(),
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture
        )
    with
    | true, steps -> Ok(Steps steps)
    | _ -> Error "expected a non-negative step count"
