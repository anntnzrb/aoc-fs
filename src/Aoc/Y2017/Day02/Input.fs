/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day02.Input

/// The input lines, in their original order.
type Puzzle =
    {
        /// Lines from the puzzle input.
        Lines: string list
    }

/// Parses the input into the puzzle domain model.
let parse (input: string) : Result<Puzzle, string> =
    Ok
        {
            Lines = input.Replace("\r\n", "\n").TrimEnd('\n').Split('\n') |> Array.toList
        }
