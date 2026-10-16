/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day05.Input

/// Relative jump offsets in instruction order.
type Instructions = Instructions of value: int list

/// Parses a non-empty list of signed integer offsets, one per line.
let parse (input: string) : Result<Instructions, string> =
    let offset (line: string) =
        match System.Int32.TryParse line with
        | true, value -> Ok value
        | false, _ -> Error "invalid jump offset"

    match input.Trim() with
    | text when String.length text = 0 -> Error "empty instruction list"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Aoc.Parse.traverse offset
        |> Result.map Instructions
