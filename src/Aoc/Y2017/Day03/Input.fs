/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day03.Input

/// The positive number identifying a memory square.
type Square = Square of int

/// Parses one positive decimal square number, allowing surrounding whitespace.
let parse (input: string) : Result<Square, string> =
    let token = input.Trim()

    match System.Int32.TryParse token with
    | true, number when number > 0 && token |> Seq.forall System.Char.IsAsciiDigit -> Ok(Square number)
    | _ -> Error "expected a positive decimal square number"
