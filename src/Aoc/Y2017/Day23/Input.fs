/// The coprocessor's instruction grammar.
module Aoc.Y2017.Day23.Input

/// One of the eight registers a through h.
type Register = Register of char

/// An immediate integer or a register's current value.
type Operand =
    | Literal of int64
    | Read of Register

/// The four coprocessor instructions.
type Instruction =
    | Set of Register * Operand
    | Sub of Register * Operand
    | Mul of Register * Operand
    | Jump of Operand * Operand

let private register (text: string) =
    match Seq.toList text with
    | [ name ] when name >= 'a' && name <= 'h' -> Ok(Register name)
    | _ -> Error "expected register a through h"

let private operand (text: string) =
    match System.Int64.TryParse text with
    | true, value -> Ok(Literal value)
    | false, _ -> register text |> Result.map Read

let private instruction (line: string) =
    match line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries) with
    | [| "jnz"; x; y |] ->
        operand x
        |> Result.bind (fun x -> operand y |> Result.map (fun y -> Jump(x, y)))
    | [| opcode; x; y |] ->
        register x
        |> Result.bind (fun x ->
            operand y
            |> Result.bind (fun y ->
                match opcode with
                | "set" -> Ok(Set(x, y))
                | "sub" -> Ok(Sub(x, y))
                | "mul" -> Ok(Mul(x, y))
                | _ -> Error "unknown instruction"))
    | _ -> Error "expected an instruction with two operands"

/// Parses a nonempty program, rejecting malformed instructions and operands.
let parse (input: string) =
    match input.Trim() with
    | "" -> Error "empty program"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Array.fold
            (fun parsed line ->
                parsed
                |> Result.bind (fun instructions -> instruction line |> Result.map (fun item -> item :: instructions)))
            (Ok [])
        |> Result.map List.rev
