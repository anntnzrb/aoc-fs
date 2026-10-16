/// Domain model and parser for the tablet's instruction language.
module Aoc.Y2017.Day18.Input

open FParsec

/// A named integer register, initially zero.
[<Struct>]
type Register = Register of value: char

/// An instruction argument is a literal integer or a register read.
type Operand =
    | Literal of value: int64
    | Read of value: Register

/// The arithmetic updates supported by the tablet.
[<Struct>]
type Operation =
    | Assign
    | Increase
    | Product
    | Remainder

/// The seven instructions understood by the tablet.
type Instruction =
    | Sound of value: Operand
    | Update of target: Register * operation: Operation * operand: Operand
    | Receive of value: Register
    | Jump of condition: Operand * offset: Operand

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

    Aoc.Parse.run (sepEndBy1 instruction newline .>> eof) input
