module Aoc.Tests.Y2017.Day13Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day13

let private firewall =
    Gen.choose (1, 5)
    |> Gen.nonEmptyListOf
    |> Gen.map (fun ranges ->
        let layers =
            ranges
            |> List.mapi (fun depth range ->
                {
                    Input.Depth = Input.Depth(uint32 depth)
                    Input.Range = Input.Range(uint32 range)
                })

        let text =
            ranges
            |> List.mapi (fun depth range -> $"{depth}: {range}")
            |> String.concat "\n"

        text, layers)

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 10
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day13"
        [
            testPropertyWithConfig
                config
                "generated firewall layers preserve depths and ranges"
                (Prop.forAll (Arb.fromGen firewall) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                config
                "solve is total on bounded firewall cycles"
                (Prop.forAll (Arb.fromGen firewall) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "stationary scanner has no safe delay" (fun () ->
                Expect.equal (Primary.solve "3: 1\n") (Error "no delay passes the firewall") "stationary scanner")
            testCase "exhausted joint cycle has no safe delay" (fun () ->
                Expect.equal
                    (Primary.solve "0: 2\n1: 2\n")
                    (Error "no delay passes the firewall")
                    "both delay parities caught")
        ]
