// approach: reverse each circular span by swapping mirrored marks, then advance the position and skip
// cost: O(n + sum of lengths) time, O(n + 256) memory, one parse and one traversal of the lengths
// trade: a locally mutated array gives direct circular indexing without copying each reversed span
module Aoc.Y2017.Day10.Primary

open Aoc.Solution
open Aoc.Y2017.Day10.Input

let private product lengths =
    let marks = Array.init 256 id

    lengths
    |> List.fold
        (fun (position, skip) (Length length) ->
            for offset in 0 .. length / 2 - 1 do
                let left = (position + offset) % 256
                let right = (position + length - 1 - offset) % 256
                let saved = marks[left]
                marks[left] <- marks[right]
                marks[right] <- saved

            (position + length + skip) % 256, (skip + 1) % 256)
        (0, 0)
    |> ignore

    marks[0] * marks[1]

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun lengths -> Answers.both (product lengths |> string) "0")
