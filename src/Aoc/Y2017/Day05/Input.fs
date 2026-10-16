/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day05.Input

/// Relative jump offsets in instruction order.
type Instructions = Instructions of int list

/// Parses a non-empty list of signed integer offsets, one per line.
let parse (input: string) : Result<Instructions, string> =
    let offset (line: string) =
        match System.Int32.TryParse line with
        | true, value -> Ok value
        | false, _ -> Error "invalid jump offset"

    match input.Trim() with
    | "" -> Error "empty instruction list"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Array.fold
            (fun acc line ->
                acc
                |> Result.bind (fun values -> offset line |> Result.map (fun value -> value :: values)))
            (Ok [])
        |> Result.map (List.rev >> Instructions)
