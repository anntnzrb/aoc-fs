/// The coprocessor's instruction grammar.
module Aoc.Y2017.Day23.Input

/// One of the eight registers a through h.
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
    | Literal of int64
    | Read of Register

/// The four coprocessor instructions.
type Instruction =
    | Set of Register * Operand
    | Sub of Register * Operand
    | Mul of Register * Operand
    | Jump of Operand * Operand

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
    | text -> text.Replace("\r\n", "\n").Split('\n') |> Aoc.Parse.traverse instruction
