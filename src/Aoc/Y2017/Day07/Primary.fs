// approach: the bottom program is the one declared name that never appears among any program's children
// cost: O(n log n) time and O(n) memory after parsing; one pass to collect names and children
// trade: immutable sets express the root invariant directly without constructing a recursive tower
module Aoc.Y2017.Day07.Primary

open Aoc.Solution
open Aoc.Y2017.Day07.Input

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun programs ->
            let names, children =
                programs
                |> List.fold
                    (fun (names, children) program ->
                        Set.add program.Name names, Set.union children (Set.ofList program.Children))
                    (Set.empty, Set.empty)

            match Set.difference names children |> Set.toList with
            | [ root ] -> Ok(Answers.both root "0")
            | _ -> Error "expected exactly one bottom program")
