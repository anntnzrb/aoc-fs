// approach: interpret once for sound recovery, then run two independent machines to blocking receives and exchange FIFO messages
// cost: O(n+t log r) time, O(n+r+q) memory for n instructions, t executed steps and q queued messages; parse then materialise, execution revisits instructions
// trade: immutable register maps and two-list queues model isolation and FIFO delivery without threads; scheduling batches work until a receive blocks
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

type private Machine =
    {
        Position: int64
        Registers: Map<char, int64>
        Inbox: int64 list * int64 list
        Sent: int64
    }

let private dequeue =
    function
    | first :: rest, rear -> Some(first, (rest, rear))
    | [], rear ->
        match List.rev rear with
        | [] -> None
        | first :: rest -> Some(first, (rest, []))

let private duet (program: Instruction array) =
    let rec run machine outgoing =
        if machine.Position < 0L || machine.Position >= int64 program.Length then
            Ok(machine, List.rev outgoing)
        else
            let read = value machine.Registers

            let next =
                { machine with
                    Position = machine.Position + 1L
                }

            match program[int machine.Position] with
            | Sound operand -> run { next with Sent = next.Sent + 1L } (read operand :: outgoing)
            | Update(target, operation, operand) ->
                update machine.Registers target operation operand
                |> Result.bind (fun registers -> run { next with Registers = registers } outgoing)
            | Receive(Register name) ->
                match dequeue machine.Inbox with
                | None -> Ok(machine, List.rev outgoing)
                | Some(number, inbox) ->
                    run
                        { next with
                            Registers = Map.add name number machine.Registers
                            Inbox = inbox
                        }
                        outgoing
            | Jump(condition, offset) ->
                let position = machine.Position + if read condition > 0L then read offset else 1L
                run { machine with Position = position } outgoing

    let deliver messages machine =
        let front, rear = machine.Inbox

        { machine with
            Inbox = front, List.fold (fun queued number -> number :: queued) rear messages
        }

    let rec exchange first second =
        run first []
        |> Result.bind (fun (first, messages) ->
            run (deliver messages second) []
            |> Result.bind (fun (second, messages) ->
                // Both copies ran to a block or termination; only a new message can restart the first.
                match messages with
                | [] -> Ok second.Sent
                | _ -> exchange (deliver messages first) second))

    let machine id =
        {
            Position = 0L
            Registers = Map.ofList [ 'p', id ]
            Inbox = [], []
            Sent = 0L
        }

    exchange (machine 0L) (machine 1L)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun instructions ->
            let program = List.toArray instructions

            recover program
            |> Result.bind (fun frequency ->
                duet program
                |> Result.map (fun sent -> Answers.both (string frequency) (string sent))))
