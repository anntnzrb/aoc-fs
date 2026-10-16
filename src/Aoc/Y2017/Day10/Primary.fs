// approach: use shared circular reversals for the numeric round and dense hash, then encode hexadecimal
// cost: O(n + sum of numeric lengths + 64 * sum of ASCII lengths) time, O(n + 256) memory; up to five text scans, two numeric and 64 suffixed ASCII-length traversals
// trade: shared knot hashing keeps reversal semantics in one place; part 1 is 0 for text without a numeric interpretation
module Aoc.Y2017.Day10.Primary

open Aoc.Solution
open Aoc.Y2017.Day10.Input

let private product lengths =
    let marks =
        lengths |> List.map (fun (Length length) -> length) |> Aoc.KnotHash.sparse 1

    marks[0] * marks[1]

let private hash text =
    text
    |> Aoc.KnotHash.dense
    |> Array.map (fun value -> value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture))
    |> String.concat ""

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun puzzle ->
            let part1 = puzzle.Lengths |> Option.map product |> Option.defaultValue 0
            Answers.both (string part1) (hash puzzle.Text))
