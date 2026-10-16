// approach: interpret the decoded instructions, retaining the last sound until a nonzero receive
// cost: O(t log r) time for t executed instructions and r registers, O(n+r) memory, one parsing pass
// trade: immutable register maps keep default-zero reads explicit; an instruction array supports relative jumps
module Aoc.Y2017.Day18.Primary

open Aoc.Solution
open Aoc.Y2017.Day18.Input

let private registerValue registers (Register name) =
    registers |> Map.tryFind name |> Option.defaultValue 0L

let private value registers =
    function
    | Literal number -> number
    | Read register -> registerValue registers register

let private update registers (Register name as target) operation operand =
    let old = registerValue registers target
    let number = value registers operand

    match operation, number with
    | Assign, _ -> Ok number
    | Increase, _ -> Ok(old + number)
    | Product, _ -> Ok(old * number)
    | Remainder, 0L -> Error "modulo by zero"
    | Remainder, _ -> Ok(old % number)
    |> Result.map (fun result -> Map.add name result registers)

let private recover (program: Instruction array) =
    let rec run position registers sound =
        if position < 0L || position >= int64 program.Length then
            Error "program terminated without recovery"
        else
            let read = value registers

            match program[int position] with
            | Sound operand -> run (position + 1L) registers (Some(read operand))
            | Update(target, operation, operand) ->
                update registers target operation operand
                |> Result.bind (fun next -> run (position + 1L) next sound)
            | Receive target when registerValue registers target <> 0L ->
                match sound with
                | Some frequency -> Ok frequency
                | None -> Error "recovery before a sound"
            | Receive _ -> run (position + 1L) registers sound
            | Jump(condition, offset) ->
                run (position + if read condition > 0L then read offset else 1L) registers sound

    run 0L Map.empty None

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (List.toArray >> recover)
        |> Result.map (fun frequency -> Answers.both (string frequency) "0")
