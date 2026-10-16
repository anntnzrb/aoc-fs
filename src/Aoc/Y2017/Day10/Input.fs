/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day10.Input

/// A span length between zero and the circle's size, inclusive.
type Length = Length of int

/// Parses comma-separated decimal lengths for a circle of 256 marks.
let parse (input: string) : Result<Length list, string> =
    input.TrimEnd('\r', '\n').Split(',')
    |> Array.fold
        (fun result token ->
            result
            |> Result.bind (fun lengths ->
                match System.Int32.TryParse token with
                | true, n when n >= 0 && n <= 256 && token |> Seq.forall System.Char.IsAsciiDigit ->
                    Ok(Length n :: lengths)
                | _ -> Error $"invalid length: %A{token}"))
        (Ok [])
    |> Result.map List.rev
