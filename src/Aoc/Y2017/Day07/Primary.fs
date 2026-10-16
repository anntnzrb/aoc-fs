// approach: find the root by name subtraction, total sub-towers bottom-up, then follow the unequal child to the wrong own weight
// cost: O(n log n) time and O(n) memory; parsing, root collection, tower ordering, totals, and one root-to-error walk
// trade: immutable maps and an explicit traversal stack keep the tree walk tail recursive and independent of input order
module Aoc.Y2017.Day07.Primary

open Aoc.Solution
open Aoc.Y2017.Day07.Input

let private bottomUp root programs =
    let rec visit pending seen ordered =
        match pending with
        | [] when Set.count seen = Map.count programs -> Ok ordered
        | [] -> Error "disconnected tower"
        | name :: rest ->
            match Set.contains name seen, Map.tryFind name programs with
            | true, _ -> Error "cycle or repeated child"
            | _, None -> Error "unknown child program"
            | false, Some program -> visit (program.Children @ rest) (Set.add name seen) (program :: ordered)

    visit [ root ] Set.empty []

let private childTotals totals program =
    program.Children
    |> List.fold
        (fun acc name ->
            acc
            |> Result.bind (fun children ->
                match Map.tryFind name totals with
                | Some total -> Ok((name, total) :: children)
                | None -> Error "missing child total"))
        (Ok [])

let private towerTotals ordered =
    ordered
    |> List.fold
        (fun acc program ->
            acc
            |> Result.bind (fun totals ->
                childTotals totals program
                |> Result.map (fun children ->
                    let total = program.Weight + (children |> List.sumBy snd)
                    Map.add program.Name total totals)))
        (Ok Map.empty)

let private correction root programs totals =
    let rec descend name expected =
        match Map.tryFind name programs, Map.tryFind name totals with
        | Some program, Some total ->
            childTotals totals program
            |> Result.bind (fun children ->
                match children |> List.groupBy snd |> List.sortByDescending (snd >> List.length) with
                | []
                | [ _ ] ->
                    match expected with
                    | Some target -> Ok(program.Weight + target - total)
                    | None -> Error "tower is already balanced"
                | [ (common, majority); (_, [ (outlier, _) ]) ] when majority.Length > 1 ->
                    descend outlier (Some common)
                | [ _; _ ] ->
                    match expected, children with
                    | Some target, [ (left, leftTotal); (right, rightTotal) ] ->
                        let childrenWeight = target - program.Weight
                        let required = childrenWeight / 2

                        match childrenWeight % 2, leftTotal = required, rightTotal = required with
                        | 0, true, false -> descend right (Some required)
                        | 0, false, true -> descend left (Some required)
                        | _ -> Error "ambiguous weight correction"
                    | _ -> Error "ambiguous weight correction"
                | _ -> Error "more than one wrong weight")
        | _ -> Error "missing tower program"

    descend root None

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun descriptions ->
            let programs =
                descriptions |> List.map (fun program -> program.Name, program) |> Map.ofList

            let names = programs |> Map.keys |> Set.ofSeq
            let children = descriptions |> List.collect _.Children |> Set.ofList

            match Set.difference names children |> Set.toList with
            | [ root ] ->
                bottomUp root programs
                |> Result.bind towerTotals
                |> Result.bind (correction root programs)
                |> Result.map (fun weight -> Answers.both root (string weight))
            | _ -> Error "expected exactly one bottom program")
