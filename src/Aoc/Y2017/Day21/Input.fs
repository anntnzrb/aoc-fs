/// Domain model and parser for square-pixel enhancement rules.
module Aoc.Y2017.Day21.Input

open FsToolkit.ErrorHandling

/// One enhancement rule, with top-down rows and a target one pixel wider.
type Rule =
    {
        Source: string list
        Target: string list
    }

/// Parses square patterns of on/off pixels and rules from size two or three.
let parse (input: string) : Result<Rule list, string> =
    let pattern (text: string) =
        let rows = text.Split('/') |> Array.toList
        let size = rows.Length

        if
            rows
            |> List.forall (fun row -> row.Length = size && row |> Seq.forall (fun c -> c = '.' || c = '#'))
        then
            Ok rows
        else
            Error "expected a square pattern of '.' and '#'"

    let rule (line: string) =
        match line.Split(" => ") with
        | [| source; target |] ->
            result {
                let! source = pattern source
                let! target = pattern target

                match source.Length, target.Length with
                | 2, 3
                | 3, 4 -> return { Source = source; Target = target }
                | _ -> return! Error "expected a 2-to-3 or 3-to-4 enhancement"
            }
        | _ -> Error "expected source => target"

    input.Trim().Split('\n') |> Aoc.Parse.traverse (fun line -> rule (line.Trim()))
