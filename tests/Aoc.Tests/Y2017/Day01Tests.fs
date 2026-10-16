module Aoc.Tests.Y2017.Day01Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day01

let private captcha =
    gen {
        let! half = Gen.choose (1, 12)
        let! digits = Gen.listOfLength (half * 2) (Gen.choose (0, 9))
        return digits |> List.map string |> String.concat "", digits
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day01"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "parser preserves circular digits"
                (Prop.forAll (Arb.fromGen captcha) (fun (text, digits) ->
                    Input.parse text |> Result.map _.Digits = Ok digits))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "solver is total on even digit sequences"
                (Prop.forAll (Arb.fromGen captcha) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
        ]
