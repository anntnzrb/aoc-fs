/// Result-returning parse boundaries shared by day parsers.
module Aoc.Parse

open FParsec

/// Applies the parser in source order, returning values in that order or the first error.
/// Stops enumerating at the first error without pulling another element and disposes the enumerator.
let traverse f (source: seq<'a>) =
    use iterator = source.GetEnumerator()
    let mutable result = Result.Ok []

    while Result.isOk result && iterator.MoveNext() do
        result <-
            result
            |> Result.bind (fun values -> f iterator.Current |> Result.map (fun value -> value :: values))

    result |> Result.map List.rev

/// Runs a parser on a string, preserving FParsec's failure message unchanged.
/// The parser is responsible for requiring eof when the whole string must be consumed.
let run (parser: Parser<'a, unit>) text =
    match FParsec.CharParsers.run parser text with
    | Success(value, _, _) -> Result.Ok value
    | Failure(message, _, _) -> Result.Error message
