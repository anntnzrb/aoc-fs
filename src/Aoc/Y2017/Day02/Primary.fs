// approach: sum row extrema differences and exact quotients from unordered pairs
// cost: O(sum of squared row lengths) arithmetic operations, O(n) memory; one parse and two traversals of parsed values, with pair scans
// trade: immutable folds enumerate each pair once without indexes; bigint keeps integer arithmetic exact
module Aoc.Y2017.Day02.Primary

open Aoc.Solution
open Aoc.Y2017.Day02.Input

let private difference (Row(first, rest)) =
    let smallest, largest =
        rest
        |> List.fold (fun (lo, hi) value -> min lo value, max hi value) (first, first)

    largest - smallest

let private divisionSum (Row(first, rest)) =
    let exact a b =
        if b <> 0I && a % b = 0I then Some(a / b) else None

    let quotient a b =
        exact a b |> Option.orElseWith (fun () -> exact b a) |> Option.defaultValue 0I

    first :: rest
    |> List.fold (fun (seen, total) value -> value :: seen, total + (seen |> List.sumBy (quotient value))) ([], 0I)
    |> snd

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun sheet ->
            let part1, part2 =
                sheet.Rows
                |> List.fold (fun (p1, p2) row -> p1 + difference row, p2 + divisionSum row) (0I, 0I)

            Answers.both (string part1) (string part2))
