module Aoc.Tests.Y2017.Day12Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day12

let private network =
    gen {
        let! count = Gen.choose (1, 8)
        let! edges = Gen.choose (0, count - 1) |> Gen.two |> Gen.listOf

        let neighbours id =
            id
            :: [
                for left, right in edges do
                    if left = id then
                        yield right

                    if right = id then
                        yield left
            ]
            |> List.distinct
            |> List.sort

        let programs =
            [
                for id in 0 .. count - 1 ->
                    {
                        Input.Id = Input.ProgramId(uint32 id)
                        Input.Connections = neighbours id |> List.map (uint32 >> Input.ProgramId)
                    }
            ]

        let text =
            [
                for id in 0 .. count - 1 ->
                    let connections = neighbours id |> List.map string |> String.concat ", "
                    $"%O{id} <-> %O{connections}"
            ]
            |> String.concat "\n"

        return text, programs
    }

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 12
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day12"
        [
            testPropertyWithConfig
                config
                "generated undirected declarations preserve identifiers and connections"
                (Prop.forAll (Arb.fromGen network) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                config
                "solve is total on bounded pipe networks"
                (Prop.forAll (Arb.fromGen network) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "missing program zero has no group to count" (fun () ->
                // Only program 1 exists; seeding a traversal with 0 would invent a program.
                Expect.equal (Primary.solve "1 <-> 1\n") (Error "program 0 is not declared") "missing program zero")
            testCase "mentioning program zero does not declare it" (fun () ->
                // Program 0 appears only as a connection, not as a declared program.
                Expect.equal (Primary.solve "1 <-> 0\n") (Error "program 0 is not declared") "undeclared connection")
        ]
