module Aoc.Tests.Y2017.Day06Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day06
open FsCheck.FSharp

let private banks =
    Gen.choose (1, 4)
    |> Gen.bind (fun size -> Gen.listOfLength size (Gen.choose (0, 4)))
    |> Gen.map (fun counts -> counts |> List.map string |> String.concat "\t", counts)

[<Tests>]
let tests =
    testList
        "Y2017.Day06"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "parser preserves bank order and counts"
                (Prop.forAll (Arb.fromGen banks) (fun (text, counts) ->
                    Input.parse text |> Result.map (fun (Input.Banks(first, rest)) -> first :: rest) = Ok counts))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "solver is total on small bank configurations"
                (Prop.forAll (Arb.fromGen banks) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "redistribution preserves totals above Int32.MaxValue" (fun () ->
                // Independent bigint two-bank reference: if a >= b, next is
                // (a/2, b+(a+1)/2); otherwise (a+(b+1)/2, b/2).
                // Starting with both Int32.MaxValue, the first repeat is at
                // cycle 32, revisiting cycle 30 (period 2).
                Expect.equal
                    (Primary.solve "2147483647 2147483647" |> Result.map Answers.render)
                    (Ok "32\n2\n")
                    "large block totals retain their exact cycle")
        ]
