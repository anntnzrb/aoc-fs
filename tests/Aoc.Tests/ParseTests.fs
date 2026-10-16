module Aoc.Tests.ParseTests

open Expecto
open FParsec

[<Tests>]
let tests =
    testList
        "Parse"
        [
            testCase "traverse preserves source order" (fun () ->
                let actual = [ 1; 2; 3 ] |> Aoc.Parse.traverse (fun value -> Result.Ok(value * 2))
                Expect.equal actual (Result.Ok [ 2; 4; 6 ]) "Parsed values remain in source order")

            testCase "traverse accepts an empty source" (fun () ->
                let actual =
                    Seq.empty<int> |> Aoc.Parse.traverse (fun value -> Result.Ok(value * 2))

                Expect.equal actual (Result.Ok []) "An empty source has no parsed values")

            testCase "traverse returns the first error" (fun () ->
                let parse value =
                    if value > 1 then Result.Error value else Result.Ok value

                let actual = [ 1; 2; 3 ] |> Aoc.Parse.traverse parse
                Expect.equal actual (Result.Error 2) "The first failing element supplies the error")

            testCase "traverse stops pulling at the first error" (fun () ->
                let source = Seq.initInfinite id

                let parse value =
                    if value = 2 then Result.Error value else Result.Ok value

                let actual = source |> Aoc.Parse.traverse parse
                Expect.equal actual (Result.Error 2) "The traversal stops at the failing index")

            for fail in [ false; true ] do
                testCase $"traverse disposes the source when failure is %b{fail}" (fun () ->
                    // Disposal is a side effect, so this test needs a mutable observer.
                    let mutable disposed = false

                    let source =
                        seq {
                            try
                                yield 1
                                yield 2
                            finally
                                disposed <- true
                        }

                    let parse value =
                        if fail then Result.Error value else Result.Ok value

                    let actual = source |> Aoc.Parse.traverse parse
                    let expected = if fail then Result.Error 1 else Result.Ok [ 1; 2 ]
                    Expect.equal actual expected "The traversal returns the expected result"
                    Expect.isTrue disposed "The source's finally block runs")

            testCase "run returns a parsed value" (fun () ->
                let actual = Aoc.Parse.run (pint32 .>> eof) "42"
                Expect.equal actual (Result.Ok 42) "The parser consumes a valid integer")

            testCase "run returns a parser error" (fun () ->
                match Aoc.Parse.run (pint32 .>> eof) "x" with
                | Result.Error _ -> ()
                | Result.Ok value -> failtestf "Expected an error, got %d" value)
        ]
