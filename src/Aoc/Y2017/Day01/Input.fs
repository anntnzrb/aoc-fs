/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day01.Input

/// A circular sequence of decimal digits.
type Captcha =
    {
        /// The digits in input order; the last is followed by the first.
        Digits: int list
    }

let private digit (c: char) : Result<int, string> =
    match c with
    | c when System.Char.IsAsciiDigit c -> Ok(int c - int '0')
    | c -> Error $"not a digit: %A{c}"

/// Parses one line of digits; rejects empty input and any non-digit.
let parse (input: string) : Result<Captcha, string> =
    match input.Trim() with
    | "" -> Error "empty captcha"
    | line ->
        line
        |> Seq.fold (fun acc c -> acc |> Result.bind (fun ds -> digit c |> Result.map (fun d -> d :: ds))) (Ok [])
        |> Result.map (fun ds -> { Digits = List.rev ds })
