// approach: interpret conditional updates in order, then take the largest final register value
// cost: O(n log r) time, O(n + r) memory; one parsing pass and one execution pass, then scan r registers
// trade: an immutable map directly models named registers while absent reads default to zero
module Aoc.Y2017.Day08.Primary

open Aoc.Solution
open Aoc.Y2017.Day08.Input

let private execute registers instruction =
    let read name =
        registers |> Map.tryFind name |> Option.defaultValue 0L

    let source = read instruction.Source

    let applies =
        match instruction.Comparison with
        | Greater -> source > instruction.Threshold
        | Less -> source < instruction.Threshold
        | GreaterOrEqual -> source >= instruction.Threshold
        | LessOrEqual -> source <= instruction.Threshold
        | Equal -> source = instruction.Threshold
        | NotEqual -> source <> instruction.Threshold

    let current = read instruction.Target

    let updated =
        match applies, instruction.Operation with
        | false, _ -> current
        | true, Increase -> current + instruction.Amount
        | true, Decrease -> current - instruction.Amount

    registers
    |> Map.add instruction.Source source
    |> Map.add instruction.Target updated

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun instructions ->
            let largest =
                instructions
                |> List.fold execute Map.empty
                |> Map.fold
                    (fun largest _ value -> Some(largest |> Option.map (max value) |> Option.defaultValue value))
                    None
                |> Option.defaultValue 0L

            Answers.both (string largest) "0")
