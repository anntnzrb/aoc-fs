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
        | false, _ -> bigint current
        | true, Increase -> bigint current + bigint instruction.Amount
        | true, Decrease -> bigint current - bigint instruction.Amount

    if updated < bigint System.Int64.MinValue || updated > bigint System.Int64.MaxValue then
        Error "register overflow"
    else
        let updated = int64 updated

        let registers =
            registers
            |> Map.add instruction.Source source
            |> Map.add instruction.Target updated

        Ok(registers, max highest updated)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun instructions ->
            instructions
            |> List.fold
                (fun state instruction -> state |> Result.bind (fun state -> execute state instruction))
                (Ok(Map.empty, 0L)))
        |> Result.map (fun (registers, highest) ->

            let largest =
                if Map.isEmpty registers then
                    0L
                else
                    registers |> Map.values |> Seq.max

            Answers.both (string largest) (string highest))
