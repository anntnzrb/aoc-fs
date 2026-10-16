// approach: interpret each instruction literally and count executed multiplications
// cost: O(t) time for t executed instructions, O(n) memory for n instructions, one parse pass
// trade: a local register array models the machine directly without allocating state per instruction
module Aoc.Y2017.Day23.Primary

open Aoc.Solution
open Aoc.Y2017.Day23.Input

let private execute (program: Instruction array) =
    let registers = Array.zeroCreate<int64> 8
    let index (Register name) = int name - int 'a'

    let value =
        function
        | Literal number -> number
        | Read register -> registers[index register]

    let mutable position = 0L
    let mutable multiplications = 0L

    while position >= 0L && position < int64 program.Length do
        let mutable offset = 1L

        match program[int position] with
        | Set(register, source) -> registers[index register] <- value source
        | Sub(register, source) -> registers[index register] <- registers[index register] - value source
        | Mul(register, source) ->
            registers[index register] <- registers[index register] * value source
            multiplications <- multiplications + 1L
        | Jump(condition, distance) ->
            if value condition <> 0L then
                offset <- value distance

        position <- position + offset

    multiplications

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun instructions -> Answers.both (instructions |> List.toArray |> execute |> string) "0")
