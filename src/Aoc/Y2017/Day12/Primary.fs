// approach: follow pipes from zero for its group size, then start a new group at each still-unvisited program
// cost: O((V + E) log(V + 2)) time, O(V + E) memory; V programs and E connections; one parse, declaration validation/index passes, then one visit per program and pipe
// trade: immutable maps and a visited set handle arbitrary identifiers and cycles without indexed mutable state
module Aoc.Y2017.Day12.Primary

open Aoc.Solution
open Aoc.Y2017.Day12.Input
open FsToolkit.ErrorHandling

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
        result {
            let! programs = Input.parse input
            let unwrap (ProgramId id) = id

            let pipes =
                programs
                |> List.map (fun program -> unwrap program.Id, program.Connections |> List.map unwrap)
                |> Map.ofList

            do! Result.requireTrue "program 0 is not declared" (Map.containsKey 0u pipes)
            let zeroGroup = group pipes Set.empty 0u

            let _, groups =
                programs
                |> List.fold
                    (fun (seen, count) program ->
                        let id = unwrap program.Id

                        match Set.contains id seen with
                        | true -> seen, count
                        | false -> group pipes seen id, count + 1)
                    (zeroGroup, 1)

            return Answers.both (zeroGroup |> Set.count |> string) (string groups)
        }
