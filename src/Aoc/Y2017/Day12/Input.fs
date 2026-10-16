/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day12.Input

open FParsec

/// A program's non-negative identifier.
type ProgramId = ProgramId of value: uint32

/// A program and the programs directly connected to it by pipes.
type Program =
    {
        Id: ProgramId
        Connections: ProgramId list
    }

/// Parses non-empty pipe declarations, each with at least one connection.
let parse (input: string) : Result<Program list, string> =
    let id = puint32 |>> ProgramId

    let program =
        id .>> pstring " <-> " .>>. sepBy1 id (pstring ", ")
        |>> fun (id, connections) -> { Id = id; Connections = connections }

    Aoc.Parse.run (sepEndBy1 program newline .>> eof) input
