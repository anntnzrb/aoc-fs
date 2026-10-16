// approach: follow relative jumps using each old offset, then increment the instruction just visited
// cost: O(n + s) time, O(n) memory; parse once, traverse the n offsets twice to build the array, execute s indexed jumps
// trade: local array mutation models changing instructions directly without copying the list at every step
module Aoc.Y2017.Day05.Primary

open Aoc.Solution
open Aoc.Y2017.Day05.Input

let private steps (Instructions offsets) =
    let jumps = offsets |> List.map int64 |> List.toArray
    let mutable position = 0L
    let mutable count = 0L

    while position >= 0L && position < int64 jumps.Length do
        let index = int position
        let offset = jumps[index]
        jumps[index] <- offset + 1L
        position <- position + offset
        count <- count + 1L

    count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun instructions -> Answers.both (steps instructions |> string) "0")
