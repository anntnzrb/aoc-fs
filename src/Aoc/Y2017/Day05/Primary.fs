// approach: follow old offsets, then update the visited instruction by each part's rule in separate simulations
// cost: O(n + s1 + s2) time, O(n) memory; parse once, two offset traversals per part, then indexed jumps
// trade: local array mutation models changing instructions directly without copying the list at every step
module Aoc.Y2017.Day05.Primary

open Aoc.Solution
open Aoc.Y2017.Day05.Input

let private steps update (Instructions offsets) =
    let jumps = offsets |> List.map int64 |> List.toArray
    let mutable position = 0L
    let mutable count = 0L

    while position >= 0L && position < int64 jumps.Length do
        let index = int position
        let offset = jumps[index]
        jumps[index] <- update offset
        position <- position + offset
        count <- count + 1L

    count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun instructions ->
            let strange offset =
                if offset >= 3L then offset - 1L else offset + 1L

            Answers.both
                (steps (fun offset -> offset + 1L) instructions |> string)
                (steps strange instructions |> string))
