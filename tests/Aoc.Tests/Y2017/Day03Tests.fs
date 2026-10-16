module Aoc.Tests.Y2017.Day03Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day03

let private square = Gen.choose (1, 256)

[<Tests>]
let tests =
    testList
        "Y2017.Day03"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "parser preserves positive square numbers"
                (Prop.forAll (Arb.fromGen square) (fun number -> Input.parse (string number) = Ok(Input.Square number)))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "solver is total on small square numbers"
                (Prop.forAll (Arb.fromGen square) (fun number ->
                    match Primary.solve (string number) with
                    | Ok _
                    | Error _ -> true))
        ]
