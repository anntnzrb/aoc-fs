module Aoc.Tests.Y2017.Day11Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day11

let private path =
    Gen.elements
        [
            "n", Input.North
            "ne", Input.Northeast
            "se", Input.Southeast
            "s", Input.South
            "sw", Input.Southwest
            "nw", Input.Northwest
        ]
    |> Gen.nonEmptyListOf
    |> Gen.map (fun moves ->
        let tokens, directions = List.unzip moves
        String.concat "," tokens, directions)

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 20
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day11"
        [
            testPropertyWithConfig
                config
                "generated paths preserve every direction in order"
                (Prop.forAll (Arb.fromGen path) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                config
                "solve is total on bounded hex paths"
                (Prop.forAll (Arb.fromGen path) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
        ]
