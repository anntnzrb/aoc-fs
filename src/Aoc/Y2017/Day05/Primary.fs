// approach: follow each old jump and persist the updated offset in an indexed map
// cost: O(n log n + (s1 + s2) log n) time, O(n) live memory; one text pass and one indexed-offset pass
// trade: persistent updates preserve every prior instruction state at the cost of tree allocation per jump
module Aoc.Y2017.Day05.Primary

open Aoc.Solution
open Aoc.Y2017.Day05.Input

[<TailCall>]
let rec private steps strange length position count jumps =
    if position < 0L || position >= int64 length then
        count
    else
        match Map.tryFind (int position) jumps with
        | None -> count
        | Some offset ->
            let next = if strange && offset >= 3L then offset - 1L else offset + 1L
            steps strange length (position + offset) (count + 1L) (Map.add (int position) next jumps)

let solve: Solver =
    fun input ->
        Input.parse input
        |> Result.map (fun (Instructions offsets) ->
            let jumps = offsets |> List.mapi (fun i offset -> i, int64 offset) |> Map.ofList

            Answers.both
                (steps false offsets.Length 0L 0L jumps |> string)
                (steps true offsets.Length 0L 0L jumps |> string))
