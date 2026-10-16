/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day10.Input

/// ASCII hash text and its comma-separated lengths.
type Puzzle =
    {
        /// Trimmed original text, including commas and interior spaces.
        Text: string
        /// Span lengths between zero and the circle's size, inclusive.
        Lengths: int list
    }

let private lengths (text: string) =
    text.Split(',')
    |> Aoc.Parse.traverse (fun token ->
        match System.Int32.TryParse token with
        | true, n when n <= 256 && token |> Seq.forall System.Char.IsAsciiDigit -> Ok n
        | _ -> Error $"invalid length: %A{token}")

/// Parses ASCII text after removing surrounding whitespace; empty text is valid.
let parse (input: string) : Result<Puzzle, string> =
    let text = input.Trim()

    match text |> Seq.forall System.Char.IsAscii with
    | false -> Error "hash input must contain only ASCII characters"
    | true ->
        (if text.Length = 0 then Ok [] else lengths text)
        |> Result.map (fun lengths -> { Text = text; Lengths = lengths })
