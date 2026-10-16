// approach: follow pipes from zero for its group size, then start a new group at each still-unvisited program
// cost: O((V + E) log V) time, O(V + E) memory; one input parse, two declaration traversals and one visit per program and pipe
// trade: immutable maps and a visited set handle arbitrary identifiers and cycles without indexed mutable state
module Aoc.Y2017.Day12.Primary

open Aoc.Solution
open Aoc.Y2017.Day12.Input

let private group pipes seen start =
    let rec visit pending seen =
        match pending with
        | [] -> seen
        | id :: rest when Set.contains id seen -> visit rest seen
        | id :: rest ->
            let neighbours = pipes |> Map.tryFind id |> Option.defaultValue []
            visit (neighbours @ rest) (Set.add id seen)

    visit [ start ] seen

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun programs ->
            let pipes =
                programs
                |> List.map (fun program -> program.Id, program.Connections)
                |> Map.ofList

            let zeroGroup = group pipes Set.empty (ProgramId 0u)

            let _, groups =
                programs
                |> List.fold
                    (fun (seen, count) program ->
                        match Set.contains program.Id seen with
                        | true -> seen, count
                        | false -> group pipes seen program.Id, count + 1)
                    (zeroGroup, 1)

            Answers.both (zeroGroup |> Set.count |> string) (string groups))
