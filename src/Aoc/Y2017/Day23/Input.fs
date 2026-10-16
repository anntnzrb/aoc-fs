/// The coprocessor's instruction grammar.
module Aoc.Y2017.Day23.Input

open FsToolkit.ErrorHandling

/// One of the eight registers a through h.
[<Struct>]
type Register =
    | A
    | B
    | C
    | D
    | E
    | F
    | G
    | H

/// An immediate integer or a register's current value.
type Operand =
    | Literal of value: int64
    | Read of value: Register

/// The four coprocessor instructions.
type Instruction =
    | Set of target: Register * operand: Operand
    | Sub of target: Register * operand: Operand
    | Mul of target: Register * operand: Operand
    | Jump of condition: Operand * offset: Operand

let private register (text: string) =
    match text with
    | "a" -> Ok A
    | "b" -> Ok B
    | "c" -> Ok C
    | "d" -> Ok D
    | "e" -> Ok E
    | "f" -> Ok F
    | "g" -> Ok G
    | "h" -> Ok H
    | _ -> Error "expected register a through h"

let private operand (text: string) =
    match System.Int64.TryParse text with
    | true, value -> Ok(Literal value)
    | false, _ -> register text |> Result.map Read

let private instruction (line: string) =
    match line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries) with
    | [| "jnz"; x; y |] ->
        result {
            let! x = operand x
            let! y = operand y
            return Jump(x, y)
        }
    | [| opcode; x; y |] ->
        result {
            let! x = register x
            let! y = operand y

            match opcode with
            | "set" -> return Set(x, y)
            | "sub" -> return Sub(x, y)
            | "mul" -> return Mul(x, y)
            | _ -> return! Error "unknown instruction"
        }
    | _ -> Error "expected an instruction with two operands"

/// Parses a nonempty program, rejecting malformed instructions and operands.
let parse (input: string) =
    match input.Trim() with
    | text when String.length text = 0 -> Error "empty program"
    | text -> text.Replace("\r\n", "\n").Split('\n') |> Aoc.Parse.traverse instruction
