// approach: use shared circular reversals for the numeric round and dense hash, then encode hexadecimal
// cost: O(n + sum of numeric lengths + 64 * sum of ASCII lengths) time, O(n + 256) memory; up to six text scans, one numeric and 64 suffixed ASCII-length traversals
// trade: shared knot hashing keeps reversal semantics in one place; parsing requires a numeric length list
module Aoc.Y2017.Day10.Primary

open Aoc.Solution
open Aoc.Y2017.Day10.Input
open FsToolkit.ErrorHandling

let private product lengths =
    match lengths |> Aoc.KnotHash.sparse 1 |> Array.truncate 2 with
    | [| first; second |] -> Ok(first * second)
    | _ -> Error "knot hash must contain at least two marks"

let private hash text =
    text |> Aoc.KnotHash.dense |> System.Convert.ToHexStringLower

let solve: Solver =
    fun input ->
        result {
            let! puzzle = Input.parse input
            let! part1 = product puzzle.Lengths
            return Answers.both (string part1) (hash puzzle.Text)
        }
