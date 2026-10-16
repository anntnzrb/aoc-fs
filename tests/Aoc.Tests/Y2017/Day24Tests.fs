module Aoc.Tests.Y2017.Day24Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
open Aoc.Y2017.Day24

let private components =
    gen {
        let! count = Gen.choose (1, 6)

        return!
            Gen.zip (Gen.choose (0, 5)) (Gen.choose (0, 5))
            |> Gen.map Input.Component
            |> Gen.listOfLength count
    }

let private render components =
    components
    |> List.map (fun (Input.Component(left, right)) -> $"{left}/{right}")
    |> String.concat "\n"

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 6
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day24"
        [
            testPropertyWithConfig
                config
                "parser preserves component order and duplicates"
                (Prop.forAll (Arb.fromGen components) (fun expected -> Input.parse (render expected) = Ok expected))
            testPropertyWithConfig
                config
                "small component inventories solve without throwing"
                (Prop.forAll (Arb.fromGen components) (render >> Primary.solve >> Result.isOk))
            testCase "bridge strength exceeds int32" (fun () ->
                // With M = Int32.MaxValue, 0/M then M/M has strength 3M and is the longest bridge.
                Expect.equal
                    (Primary.solve "0/2147483647\n2147483647/2147483647\n"
                     |> Result.map Answers.render)
                    (Ok "6442450941\n6442450941\n")
                    "both objectives retain the full strength")
        ]
