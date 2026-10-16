// approach: interpret each blueprint transition, storing only tape positions containing one; their count is the checksum
// cost: O(n + (s + q) log(q + 1)) expected time, O(n + q + t) memory for input size n, s steps, q states and peak t ones; one input pass
// trade: a local mutable hash set avoids allocating a persistent tape on every step while preserving a pure solver boundary
module Aoc.Y2017.Day25.Primary

open Aoc.Solution
open Aoc.Y2017.Day25.Input

let private checksum blueprint =
    let ones = System.Collections.Generic.HashSet<int>()
    let mutable cursor = 0
    let mutable state = blueprint.Start
    let mutable remaining = blueprint.Steps
    let mutable error = None

    while remaining > 0 && Option.isNone error do
        match Map.tryFind state blueprint.States with
        | None -> error <- Some "undefined machine state"
        | Some rules ->
            let action = if ones.Contains cursor then rules.One else rules.Zero

            match action.Write with
            | Zero -> ones.Remove cursor |> ignore
            | One -> ones.Add cursor |> ignore

            cursor <-
                cursor
                + (match action.Move with
                   | Left -> -1
                   | Right -> 1)

            state <- action.Next
            remaining <- remaining - 1

    match error with
    | Some message -> Error message
    | None -> Ok ones.Count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind checksum
        |> Result.map (string >> Answers.part1)
