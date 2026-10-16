/// Domain model and parser for the tablet's instruction language.
module Aoc.Y2017.Day18.Input

open FParsec

/// A named integer register, initially zero.
type Register = Register of char

/// An instruction argument is a literal integer or a register read.
type Operand =
    | Literal of int64
    | Read of Register

/// The arithmetic updates supported by the tablet.
type Operation =
    | Assign
    | Increase
    | Product
    | Remainder

/// The seven instructions understood by the tablet.
type Instruction =
    | Sound of Operand
    | Update of Register * Operation * Operand
    | Receive of Register
    | Jump of Operand * Operand

/// Parses a nonempty program, rejecting malformed instructions and operands.
let parse (input: string) : Result<Instruction list, string> =
    let register = asciiLower |>> Register
    let operand = (pint64 |>> Literal) <|> (register |>> Read)
    let gap = skipMany1 (anyOf " \t")

    let update name operation =
        pstring name
        >>. gap
        >>. pipe2 (register .>> gap) operand (fun x y -> Update(x, operation, y))

    let instruction =
        choice
            [
                pstring "snd" >>. gap >>. operand |>> Sound
                update "set" Assign
                update "add" Increase
                update "mul" Product
                update "mod" Remainder
                pstring "rcv" >>. gap >>. register |>> Receive
                pstring "jgz"
                >>. gap
                >>. pipe2 (operand .>> gap) operand (fun x y -> Jump(x, y))
            ]

    match run (sepEndBy1 instruction newline .>> eof) input with
    | Success(program, _, _) -> Result.Ok program
    | Failure(message, _, _) -> Result.Error message
