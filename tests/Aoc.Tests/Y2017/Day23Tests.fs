module Aoc.Tests.Y2017.Day23Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
open Aoc.Y2017.Day23

let private programs =
    let register =
        Gen.elements [ Input.A; Input.B; Input.C; Input.D; Input.E; Input.F; Input.G; Input.H ]

    let operand =
        Gen.oneof
            [
                Gen.choose (-5, 5) |> Gen.map (int64 >> Input.Literal)
                register |> Gen.map Input.Read
            ]

    let instruction =
        gen {
            let! target = register
            let! source = operand
            let! offset = Gen.choose (1, 3)

            return!
                Gen.elements
                    [
                        Input.Set(target, source)
                        Input.Sub(target, source)
                        Input.Mul(target, source)
                        Input.Jump(source, Input.Literal(int64 offset))
                    ]
        }

    gen {
        let! count = Gen.choose (1, 8)
        return! Gen.listOfLength count instruction
    }

let private render program =
    let register =
        function
        | Input.A -> "a"
        | Input.B -> "b"
        | Input.C -> "c"
        | Input.D -> "d"
        | Input.E -> "e"
        | Input.F -> "f"
        | Input.G -> "g"
        | Input.H -> "h"

    let operand =
        function
        | Input.Literal value -> string value
        | Input.Read source -> register source

    program
    |> List.map (function
        | Input.Set(target, source) -> $"set %O{register target} %O{operand source}"
        | Input.Sub(target, source) -> $"sub %O{register target} %O{operand source}"
        | Input.Mul(target, source) -> $"mul %O{register target} %O{operand source}"
        | Input.Jump(condition, offset) -> $"jnz %O{operand condition} %O{operand offset}")
    |> String.concat "\n"

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 50
        endSize = 8
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day23"
        [
            testPropertyWithConfig
                config
                "parser preserves instructions and operand kinds"
                (Prop.forAll (Arb.fromGen programs) (fun expected -> Input.parse (render expected) = Ok expected))
            testPropertyWithConfig
                config
                "bounded forward-jump programs return a result without throwing"
                (Prop.forAll (Arb.fromGen programs) (fun program ->
                    match Primary.solve (render program) with
                    | Ok _
                    | Error _ -> true))
            testCase "rejects unsupported factor-loop structure" (fun () ->
                Expect.equal
                    (Primary.solve "set d 2\nset e 2\n")
                    (Error "unsupported nested factor-loop structure")
                    "A candidate loop must satisfy the complete structural proof")
            testCase "rejects registers outside the machine" (fun () ->
                Expect.isError (Input.parse "set z 1\n") "Only registers a through h exist")
            // The subtractions cross either int64 boundary; both products are outside that range.
            for name, program in
                [
                    "subtraction above maximum", "set h 9223372036854775807\nsub h -1\n"
                    "subtraction below minimum", "set h -9223372036854775808\nsub h 1\n"
                    "multiplication above maximum", "set h -9223372036854775808\nmul h -1\n"
                    "multiplication below minimum", "set h 9223372036854775807\nmul h -2\n"
                ] do
                testCase name (fun () ->
                    Expect.equal (Primary.solve program) (Error "register overflow") "Overflow returns an error")
            testCase "rejects overflow when only part two executes it" (fun () ->
                // a=0 skips the arithmetic; a=1 reaches MaxValue + 1.
                Expect.equal
                    (Primary.solve "jnz a 2\njnz 1 3\nset h 9223372036854775807\nsub h -1\n")
                    (Error "register overflow")
                    "Both interpreter runs use the same overflow policy")
            testCase "accepts arithmetic at the integer boundaries" (fun () ->
                // MinValue - (-1) - (-MaxValue) = 0; MaxValue * 1 stays representable.
                let program =
                    "set h -9223372036854775808\nsub h -1\nsub h -9223372036854775807\nset h 9223372036854775807\nmul h 1\n"

                Expect.equal
                    (Primary.solve program |> Result.map Answers.render)
                    (Ok "1\n9223372036854775807\n")
                    "Representable boundary arithmetic is preserved")
        ]
