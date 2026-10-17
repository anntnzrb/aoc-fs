module Aoc.Tests.Y2017.Day08Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
open Aoc.Y2017.Day08

let private program =
    gen {
        let! target = Gen.elements [ "a"; "bc"; "Z" ]
        let! source = Gen.elements [ "a"; "bc"; "Z" ]
        let! op, operation = Gen.elements [ "inc", Input.Increase; "dec", Input.Decrease ]

        let! cmp, comparison =
            Gen.elements
                [
                    ">", Input.Greater
                    "<", Input.Less
                    ">=", Input.GreaterOrEqual
                    "<=", Input.LessOrEqual
                    "==", Input.Equal
                    "!=", Input.NotEqual
                ]

        let! amount = Gen.choose (-20, 20)
        let! threshold = Gen.choose (-20, 20)

        return
            $"%O{target} %O{op} %O{amount} if %O{source} %O{cmp} %O{threshold}",
            {
                Input.Target = target
                Input.Operation = operation
                Input.Amount = int64 amount
                Input.Source = source
                Input.Comparison = comparison
                Input.Threshold = int64 threshold
            }
    }
    |> Gen.nonEmptyListOf
    |> Gen.map (fun instructions ->
        let lines, expected = List.unzip instructions
        String.concat "\n" lines, expected)

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 12
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day08"
        [
            testCase "decrement by minimum overflows" (fun () ->
                Expect.equal
                    (Primary.solve "a dec -9223372036854775808 if a == 0")
                    (Error "register overflow")
                    "zero minus MinValue is above MaxValue")
            testCase "successive increments detect overflow" (fun () ->
                Expect.equal
                    (Primary.solve "a inc 9223372036854775807 if a == 0\na inc 1 if a > 0")
                    (Error "register overflow")
                    "MaxValue plus one is outside the register range")
            testCase "skipped overflowing update succeeds" (fun () ->
                Expect.equal
                    (Primary.solve "a dec -9223372036854775808 if a != 0")
                    (Ok(Answers.both "0" "0"))
                    "a false condition never evaluates its update")
            testPropertyWithConfig
                config
                "generated instructions preserve their fields and order"
                (Prop.forAll (Arb.fromGen program) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                config
                "solve is total on bounded register programs"
                (Prop.forAll (Arb.fromGen program) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "final maximum can be negative" (fun () ->
                // The only register changes from zero to -5; its all-time maximum remains zero.
                Expect.equal
                    (Primary.solve "a dec 5 if a == 0" |> Result.map Answers.render)
                    (Ok "-5\n0\n")
                    "the final maximum has no zero floor")
            testCase "condition-only registers retain zero" (fun () ->
                // a becomes -5, but b is only read and remains zero, so both maxima are zero.
                Expect.equal
                    (Primary.solve "a dec 5 if b == 0" |> Result.map Answers.render)
                    (Ok "0\n0\n")
                    "condition-only registers count in the final maximum")
        ]
