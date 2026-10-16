/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day10.Input

/// A span length between zero and the circle's size, inclusive.
type Length = Length of int

/// ASCII hash text and its optional interpretation as comma-separated lengths.
type Puzzle =
    {
        /// Trimmed original text, including commas and interior spaces.
        Text: string
        /// None when the text has no valid part-one length interpretation.
        Lengths: Length list option
    }

let private lengths (text: string) =
    text.Split(',')
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

/// Parses ASCII text after removing surrounding whitespace; empty text is valid.
let parse (input: string) : Result<Puzzle, string> =
    let text = input.Trim()

    match text |> Seq.forall System.Char.IsAscii with
    | false -> Error "hash input must contain only ASCII characters"
    | true ->
        Ok
            {
                Text = text
                Lengths =
                    if text = "" then
                        Some []
                    else
                        text |> lengths |> Result.toOption
            }
