// approach: follow the bidirectional pipes from program zero, visiting each reachable program once
// cost: O((V + E) log V) time, O(V + E) memory; one parse pass and one traversal of the declarations to build adjacency
// trade: immutable maps and a visited set handle arbitrary identifiers and cycles without indexed mutable state
module Aoc.Y2017.Day12.Primary

open Aoc.Solution
open Aoc.Y2017.Day12.Input

let private group pipes start =
    let rec visit pending seen =
        match pending with
        | [] -> seen
        | id :: rest when Set.contains id seen -> visit rest seen
        | id :: rest ->
            let neighbours = pipes |> Map.tryFind id |> Option.defaultValue []
            visit (neighbours @ rest) (Set.add id seen)

    visit [ start ] Set.empty

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun programs ->
            let pipes =
                programs
                |> List.map (fun program -> program.Id, program.Connections)
                |> Map.ofList

            Answers.both (group pipes (ProgramId 0u) |> Set.count |> string) "0")
