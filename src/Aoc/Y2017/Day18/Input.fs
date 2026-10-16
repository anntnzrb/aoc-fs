/// Domain model and parser for the tablet's instruction language.
module Aoc.Y2017.Day18.Input

open FParsec

/// A named integer register, initially zero.
type Register = Register of char

/// An instruction argument is a literal integer or a register read.
type Operand =
    | Literal of int64
    | Read of Register

/// The seven instructions understood by the tablet.
type Instruction =
    | Sound of Operand
    | Set of Register * Operand
    | Add of Register * Operand
    | Multiply of Register * Operand
    | Modulo of Register * Operand
    | Receive of Operand
    | Jump of Operand * Operand

/// Parses a nonempty program, rejecting malformed instructions and operands.
let parse (input: string) : Result<Instruction list, string> =
    let register = asciiLower |>> Register
    let operand = (pint64 |>> Literal) <|> (register |>> Read)
    let gap = skipMany1 (anyOf " \t")

    let unary name construct =
        pstring name >>. gap >>. operand |>> construct

    let binary name construct =
        pstring name
        >>. gap
        >>. pipe2 (register .>> gap) operand (fun x y -> construct (x, y))

    let instruction =
        choice
            [
                unary "snd" Sound
                binary "set" Set
                binary "add" Add
                binary "mul" Multiply
                binary "mod" Modulo
                unary "rcv" Receive
                pstring "jgz"
                >>. gap
                >>. pipe2 (operand .>> gap) operand (fun x y -> Jump(x, y))
            ]

    match run (sepEndBy1 instruction newline .>> eof) input with
    | Success(program, _, _) -> Result.Ok program
    | Failure(message, _, _) -> Result.Error message
