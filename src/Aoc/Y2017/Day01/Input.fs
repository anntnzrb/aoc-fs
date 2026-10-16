/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day01.Input

/// A circular sequence of decimal digits.
type Captcha =
    {
        /// The digits in input order; the last is followed by the first.
        Digits: int list
    }

let private digit (c: char) : Result<int, string> =
    if System.Char.IsAsciiDigit c then
        Ok(int c - int '0')
    else
        Error $"not a digit: {c}"

/// Parses one line of digits; rejects empty input and any non-digit.
let parse (input: string) : Result<Captcha, string> =
    match input.Trim() with
    | "" -> Error "empty captcha"
    | line -> line |> Aoc.Parse.traverse digit |> Result.map (fun ds -> { Digits = ds })
