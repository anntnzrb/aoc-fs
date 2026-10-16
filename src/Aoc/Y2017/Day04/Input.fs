/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day04.Input

/// A passphrase containing lowercase words in their original order.
type Passphrase = Passphrase of string list

let private passphrase (line: string) =
    let words =
        line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries) |> Array.toList

    match words with
    | [] -> Error "empty passphrase"
    | words when words |> List.forall (Seq.forall (fun c -> c >= 'a' && c <= 'z')) -> Ok(Passphrase words)
    | _ -> Error "passphrase words must contain only lowercase letters"

/// Parses the input into the puzzle domain model.
let parse (input: string) : Result<Passphrase list, string> =
    input.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
    |> Aoc.Parse.traverse passphrase
