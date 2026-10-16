module Aoc.Tests.Fixtures

open System
open System.IO
open Expecto
open Aoc
open Aoc.Solution

let check (expected: string) (rendered: string) : Result<unit, string> =
    if expected.Length = 0 then
        Error "empty expectation"
    elif not (expected.EndsWith('\n')) then
        Error "unterminated expectation"
    else
        let expectedLines =
            expected.Substring(0, expected.Length - 1).Split('\n') |> Array.toList

        let actualLines =
            rendered.Split('\n') |> Array.toList |> List.truncate expectedLines.Length

        if expectedLines = actualLines then
            Ok()
        else
            Error(sprintf "expected lines: %A; actual lines: %A" expectedLines actualLines)

[<Tests>]
let fixtures =
    testList
        "Fixtures"
        [
            for KeyValue(key, solver) in Registry.solvers do
                let directory =
                    Path.Combine(Repository.root, "fixtures", string key.Year, sprintf "%02d" key.Day)

                if Directory.Exists directory then
                    let variant =
                        match key.Variant with
                        | Primary -> "primary"
                        | Exploration name -> name

                    for inputPath in Directory.GetFiles(directory, "*.in") |> Array.sort do
                        let name =
                            sprintf
                                "%d/%02d/%s/%s"
                                key.Year
                                key.Day
                                variant
                                (Path.GetFileName inputPath |> Option.ofObj |> Option.defaultValue inputPath)

                        testCase name (fun () ->
                            let outputPath =
                                Path.ChangeExtension(inputPath, ".out")
                                |> Option.ofObj
                                |> Option.defaultValue (inputPath + ".out")

                            let expected = File.ReadAllText outputPath

                            match File.ReadAllText inputPath |> solver with
                            | Error message -> failtest message
                            | Ok answers ->
                                match answers |> Answers.render |> check expected with
                                | Ok() -> ()
                                | Error message -> failtest message)
        ]

[<Tests>]
let checkTests =
    testList
        "Fixtures.check"
        [
            testCase "exact match" (fun () -> Expect.equal (check "1\n2\n" "1\n2\n") (Ok()) "Both lines agree")
            testCase "extra rendered lines accepted" (fun () ->
                Expect.equal (check "1\n" "1\n2\n") (Ok()) "Part 1 agrees")
            testCase "mismatch rejected" (fun () -> Expect.isError (check "1\n" "2\n") "Different answers fail")
            testCase "fewer rendered lines rejected" (fun () ->
                Expect.isError (check "1\n2\n" "1\n") "Missing answers fail")
            testCase "empty expectation rejected" (fun () ->
                Expect.equal (check "" "1\n") (Error "empty expectation") "Empty expectations fail")
            testCase "unterminated expectation rejected" (fun () ->
                Expect.equal (check "1" "1\n") (Error "unterminated expectation") "Require trailing LF")
        ]
