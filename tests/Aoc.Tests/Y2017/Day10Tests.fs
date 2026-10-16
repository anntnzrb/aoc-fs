module Aoc.Tests.Y2017.Day10Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day10

let private lengths = Gen.choose (0, 256) |> Gen.listOf

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 20
        endSize = 4
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day10"
        [
            testPropertyWithConfig
                config
                "generated length lists preserve text and values"
                (Prop.forAll (Arb.fromGen lengths) (fun values ->
                    let text = values |> List.map string |> String.concat ","

                    Input.parse text =
                        Ok
                            {
                                Input.Text = text
                                Input.Lengths = values
                            }))
            testPropertyWithConfig
                config
                "solve is total on bounded length lists"
                (Prop.forAll (Arb.fromGen lengths) (fun values ->
                    let text = values |> List.map string |> String.concat ","

                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "non-numeric text is not a length list" (fun () ->
                // Letters cannot represent any comma-separated length in 0..256.
                Expect.isError (Primary.solve "not-a-length-list") "require numeric lengths")
        ]
