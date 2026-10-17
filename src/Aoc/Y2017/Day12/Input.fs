/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day12.Input

open FParsec
open FsToolkit.ErrorHandling

/// A program's non-negative identifier.
type ProgramId = ProgramId of value: uint32

/// A program and the programs directly connected to it by pipes.
type Program =
    {
        Id: ProgramId
        Connections: ProgramId list
    }

/// Parses unique declarations with complete, reciprocal pipe connections.
let parse (input: string) : Result<Program list, string> =
    let id = puint32 |>> ProgramId

    let program =
        id .>> pstring " <-> " .>>. sepBy1 id (pstring ", ")
        |>> fun (id, connections) -> { Id = id; Connections = connections }

    result {
        let! programs = Aoc.Parse.run (sepEndBy1 program newline .>> eof) input

        let connections =
            programs
            |> List.map (fun program -> program.Id, Set.ofList program.Connections)
            |> Map.ofList

        do! Result.requireTrue "duplicate program id" (connections.Count = programs.Length)

        do!
            programs
            |> List.forall (fun program ->
                program.Connections |> List.forall (fun id -> Map.containsKey id connections))
            |> Result.requireTrue "connection to an undeclared program"

        do!
            programs
            |> List.forall (fun program ->
                program.Connections
                |> List.forall (fun id ->
                    connections
                    |> Map.tryFind id
                    |> Option.exists (fun neighbours -> Set.contains program.Id neighbours)))
            |> Result.requireTrue "non-reciprocal connection"

        return programs
    }
