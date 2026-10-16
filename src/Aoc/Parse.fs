/// Result-returning parse boundaries shared by day parsers.
module Aoc.Parse

open FParsec

/// Applies the parser in source order, returning values in that order or the first error.
/// Stops enumerating at the first error without pulling another element and disposes the enumerator.
let traverse f (source: 'a seq) =
    use iterator = source.GetEnumerator()

    let rec loop acc =
        if iterator.MoveNext() then
            match f iterator.Current with
            | Result.Ok value -> loop (value :: acc)
            | Result.Error error -> Result.Error error
        else
            Result.Ok(List.rev acc)

    loop []

/// Runs a parser on a string, preserving FParsec's failure message unchanged.
/// The parser is responsible for requiring eof when the whole string must be consumed.
let run (parser: Parser<'a, unit>) text =
    match FParsec.CharParsers.run parser text with
    | Success(value, _, _) -> Result.Ok value
    | Failure(message, _, _) -> Result.Error message
