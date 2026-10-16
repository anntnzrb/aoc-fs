// approach: interpret the decoded instructions, retaining the last sound until a nonzero receive
// cost: O(t log r) time for t executed instructions and r registers, O(n+r) memory, one parsing pass
// trade: immutable register maps keep default-zero reads explicit; an instruction array supports relative jumps
module Aoc.Y2017.Day18.Primary

open Aoc.Solution
open Aoc.Y2017.Day18.Input

let private value registers =
    function
    | Literal number -> number
    | Read(Register name) -> registers |> Map.tryFind name |> Option.defaultValue 0L

let private recover (program: Instruction array) =
    let rec run position registers sound =
        if position < 0L || position >= int64 program.Length then
            Error "program terminated without recovery"
        else
            let read = value registers
            let store (Register name) number = Map.add name number registers

            match program[int position] with
            | Sound operand -> run (position + 1L) registers (Some(read operand))
            | Set(target, operand) -> run (position + 1L) (store target (read operand)) sound
            | Add((Register name as target), operand) ->
                let old = registers |> Map.tryFind name |> Option.defaultValue 0L
                run (position + 1L) (store target (old + read operand)) sound
            | Multiply((Register name as target), operand) ->
                let old = registers |> Map.tryFind name |> Option.defaultValue 0L
                run (position + 1L) (store target (old * read operand)) sound
            | Modulo((Register name as target), operand) ->
                let old = registers |> Map.tryFind name |> Option.defaultValue 0L

                match read operand with
                | 0L -> Error "modulo by zero"
                | divisor -> run (position + 1L) (store target (old % divisor)) sound
            | Receive operand when read operand <> 0L ->
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
