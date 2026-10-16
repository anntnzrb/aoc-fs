module Aoc.Tests.Y2017.Day04Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day04

let private phrases =
    let word =
        Gen.choose (1, 6)
        |> Gen.bind (fun size -> Gen.listOfLength size (Gen.elements [ 'a' .. 'z' ]))
        |> Gen.map (List.toArray >> System.String)

    let phrase = Gen.choose (1, 6) |> Gen.bind (fun size -> Gen.listOfLength size word)

    Gen.choose (1, 6)
    |> Gen.bind (fun size -> Gen.listOfLength size phrase)
    |> Gen.map (fun words -> words |> List.map (String.concat " ") |> String.concat "\n", words)

[<Tests>]
let tests =
    testList
        "Y2017.Day04"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "parser preserves phrase and word order"
                (Prop.forAll (Arb.fromGen phrases) (fun (text, words) ->
                    Input.parse text
                    |> Result.map (fun phrases -> phrases |> List.map (fun (Input.Passphrase words) -> words))
                        =
                        Ok words))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "solver is total on lowercase passphrases"
                (Prop.forAll (Arb.fromGen phrases) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
        ]
