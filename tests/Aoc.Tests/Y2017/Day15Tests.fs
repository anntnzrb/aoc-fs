module Aoc.Tests.Y2017.Day15Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day15

let private generators =
    gen {
        let! a = Gen.choose (0, 2147483646)
        let! b = Gen.choose (0, 2147483646)
        let! newline = Gen.elements [ "\n"; "\r\n" ]
        let! suffix = Gen.elements [ ""; newline ]
        return $"Generator A starts with %O{a}%O{newline}Generator B starts with %O{b}%O{suffix}", (int64 a, int64 b)
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day15"
        [
            testProperty
                "parser preserves labelled generator seeds"
                (Prop.forAll (Arb.fromGen generators) (fun (text, (a, b)) ->
                    match Input.parse text with
                    | Ok {
                             A = Input.Seed parsedA
                             B = Input.Seed parsedB
                         } -> parsedA = a && parsedB = b
                    | Error _ -> false))
        ]
