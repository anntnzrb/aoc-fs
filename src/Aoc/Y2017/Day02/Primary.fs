// approach: fold each row's minimum and maximum together, then sum their differences
// cost: O(n) arithmetic operations, O(n) memory; one parse and one traversal of the parsed values
// trade: nonempty rows seed the extrema without sentinels; bigint keeps integer arithmetic exact
module Aoc.Y2017.Day02.Primary

open Aoc.Solution
open Aoc.Y2017.Day02.Input

let private difference (Row(first, rest)) =
    let smallest, largest =
        rest
        |> List.fold (fun (lo, hi) value -> min lo value, max hi value) (first, first)

    largest - smallest

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun sheet -> Answers.both (sheet.Rows |> List.sumBy difference |> string) "0")
