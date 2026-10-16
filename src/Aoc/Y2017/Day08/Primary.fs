// approach: interpret conditional updates while tracking the highest value, then find the final maximum
// cost: O(n log r) time, O(n + r) memory; parse, reverse and execute n instructions, then scan r registers
// trade: an immutable map directly models named registers while absent reads default to zero
module Aoc.Y2017.Day08.Primary

open Aoc.Solution
open Aoc.Y2017.Day08.Input

let private execute (registers, highest) instruction =
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

    let registers =
        registers
        |> Map.add instruction.Source source
        |> Map.add instruction.Target updated

    registers, max highest updated

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun instructions ->
            let registers, highest = instructions |> List.fold execute (Map.empty, 0L)

            let largest =
                registers
                |> Map.fold
                    (fun largest _ value -> Some(largest |> Option.map (max value) |> Option.defaultValue value))
                    None
                |> Option.defaultValue 0L

            Answers.both (string largest) (string highest))
