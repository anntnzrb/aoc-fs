module Aoc.Tests.Y2017.Day09Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day09

let private stream =
    let garbage =
        Gen.elements [ 'a'; '{'; '}'; ','; '<'; '>'; '!' ]
        |> Gen.listOf
        |> Gen.map (fun characters ->
            let text =
                characters
                |> List.map (fun character ->
                    if character = '!' || character = '>' then
                        "!" + string character
                    else
                        string character)
                |> String.concat ""

            let remaining =
                characters
                |> List.filter (fun character -> character <> '!' && character <> '>')
                |> List.toArray
                |> System.String

            "<" + text + ">", Input.Garbage remaining)

    let rec group depth =
        let child =
            if depth = 0 then
                garbage
            else
                Gen.oneof [ garbage; group (depth - 1) ]

        child
        |> Gen.resize 3
        |> Gen.listOf
        |> Gen.map (fun children ->
            let texts, items = List.unzip children
            "{" + String.concat "," texts + "}", Input.Group items)

    group 2

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 40
        endSize = 8
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day09"
        [
            testPropertyWithConfig
                config
                "generated groups and escaped garbage parse exactly"
                (Prop.forAll (Arb.fromGen stream) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                config
                "solve is total on bounded nested streams"
                (Prop.forAll (Arb.fromGen stream) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
        ]
