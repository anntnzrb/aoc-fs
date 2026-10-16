/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day06.Input

/// Non-empty banks of non-negative block counts, in index order.
type Banks = Banks of first: int * rest: int list

let private blocks (token: string) =
    match System.Int32.TryParse token with
    | true, count when count >= 0 -> Ok count
    | _ -> Error $"invalid block count: %s{token}"

/// Parses whitespace-separated counts; rejects empty input and invalid counts.
let parse (input: string) : Result<Banks, string> =
    input.Split([| ' '; '\t'; '\r'; '\n' |], System.StringSplitOptions.RemoveEmptyEntries)
    |> Array.fold
        (fun counts token ->
            counts
            |> Result.bind (fun counts -> blocks token |> Result.map (fun count -> count :: counts)))
        (Ok [])
    |> Result.bind (fun counts ->
        match List.rev counts with
        | [] -> Error "empty bank sequence"
        | first :: rest -> Ok(Banks(first, rest)))
