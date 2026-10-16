module Aoc.Analyzers.Tests.RulesTests

open Expecto
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open FSharp.Compiler.Diagnostics

// A separate suite keeps compiler-service dependencies out of the puzzle test process.
let private options =
    lazy
        (mkOptionsFromProject "net10.0" [ { Name = "FParsec"; Version = "1.1.1" } ]
         |> fun pending -> pending.GetAwaiter().GetResult())

let private diagnostics fileName source =
    let context = getContext options.Value ("module Sample\n" + source)

    let errors =
        context.CheckFileResults.Diagnostics
        |> Array.filter (fun diagnostic -> diagnostic.Severity = FSharpDiagnosticSeverity.Error)

    Expect.isEmpty errors "The snippet must type check before its analyzer result counts."

    let projectOptions =
        match context.ProjectOptions with
        | AnalyzerProjectOptions.BackgroundCompilerOptions options ->
            AnalyzerProjectOptions.BackgroundCompilerOptions
                { options with
                    ProjectFileName = "/repo/tests/Aoc.Analyzers.Tests/Tests.fsproj"
                }
        | options -> options

    Aoc.Analyzers.Rules.analyze
        { context with
            FileName = fileName
            ProjectOptions = projectOptions
        }
    |> Async.RunSynchronously

let private primary = "/repo/src/Aoc/Y2017/Day01/Primary.fs"

[<Tests>]
let tests =
    testList
        "Repository analyzers"
        [
            for name, source, count in
                [
                    "value", "let value = 1", 1
                    "function", "let value x = x", 1
                    "alias", "type Value = int", 1
                    "union", "type Value = First | Second", 3
                    "record", "type Value = { First: int; Second: int }", 3
                    "documented union",
                    "/// A choice.\ntype Value =\n    /// First.\n    | First\n    /// Second.\n    | Second",
                    0
                    "documented record",
                    "/// A pair.\ntype Value =\n    {\n        /// First.\n        First: int\n        /// Second.\n        Second: int\n    }",
                    0
                    "documented function", "/// Identity.\nlet value x = x", 0
                    "private declarations", "let private value = 1\ntype private Value = { Field: int }", 0
                    "internal declarations", "let internal value = 1\ntype internal Value = First", 0
                    "private module", "module private Hidden =\n    let value = 1\n    type Value = { Field: int }", 0
                    "local binding", "/// Identity.\nlet value x =\n    let local = x\n    local", 0
                    "private representation", "/// Opaque.\ntype Value = private { Field: int }", 0
                    "private union representation", "/// Opaque.\ntype Value = private First of int", 0
                    "union payload", "/// A value.\ntype Value =\n    /// Payload.\n    | Value of payload: int", 0
                ] do
                testCase ("AOC007 " + name) (fun () ->
                    let found =
                        diagnostics "/repo/src/Aoc/Shared.fs" source
                        |> List.filter (fun d -> d.Code = "AOC007")

                    Expect.equal found.Length count "Public declarations only")
            for path, count in
                [
                    "/repo/src/Aoc/Registry.fs", 1
                    "/repo/src/Aoc/Y2017/Registry.fs", 1
                    "/repo/src/Aoc/Y2017/Day01/Primary.fs", 0
                    "/repo/src/Aoc/Y2017/Day01/Input.fs", 0
                    "/repo/src/Aoc/Y2017/Day01/Recursive.fs", 0
                    "/repo/app/Aoc.Ops/Command.fs", 0
                    "/repo/tests/Aoc.Tests/Example.fs", 0
                ] do
                testCase ("AOC007 scope " + path) (fun () ->
                    let found =
                        diagnostics path "let value = 1" |> List.filter (fun d -> d.Code = "AOC007")

                    Expect.equal found.Length count "Registries remain checked; their templates already emit docs")
            for code, bad, good in
                [
                    "AOC001", "let mutable count = 0", "let count = 0"
                    "AOC002",
                    "let update (values: int array) = values[0] <- 1",
                    "let read (values: int array) = values[0]"
                    "AOC003",
                    "let values = System.Collections.Generic.Dictionary<int, int>()",
                    "let values = Map.empty<int, int>"
                    "AOC004", "let value = List.head [1]", "let value = List.tryHead [1]"
                    "AOC005", "let value = id <| 1", "let value = 1 |> id"
                    "AOC006", "type Counter() = member _.Value = 1", "type Counter = { Value: int }"
                ] do
                testCase (code + " rejects violation") (fun () ->
                    Expect.isTrue
                        (diagnostics primary bad
                         |> List.exists (fun diagnostic -> diagnostic.Code = code))
                        code)

                testCase (code + " accepts functional alternative") (fun () ->
                    Expect.isEmpty (diagnostics primary good) code)
            for name, source in
                [
                    "hash set", "let value = System.Collections.Generic.HashSet<int>()"
                    "resize array", "let value = ResizeArray<int>()"
                    "queue", "let value = System.Collections.Generic.Queue<int>()"
                    "stack", "let value = System.Collections.Generic.Stack<int>()"
                    "array set", "let update values = Array.set values 0 1"
                    "array2d set", "let update values = Array2D.set values 0 0 1"
                    "array fill", "let update values = Array.fill values 0 1 0"
                    "array blit", "let update values target = Array.blit values 0 target 0 1"
                    "zero create", "let value = Array.zeroCreate<int> 3"
                    "zero create 2d", "let value = Array2D.zeroCreate<int> 2 3"
                ] do
                testCase ("mutable operation " + name) (fun () ->
                    Expect.isTrue
                        (diagnostics primary source
                         |> List.exists (fun diagnostic -> diagnostic.Code = "AOC003"))
                        name)
            for name, source in
                [
                    "seq head", "let value = Seq.head [1]"
                    "list item", "let value = List.item 0 [1]"
                    "option get", "let value = Option.get (Some 1)"
                    "option alias", "module O = Option\nlet value = O.get (Some 1)"
                    "first class option get", "let value = Option.get<int>"
                    "failwith", "let value () = failwith \"bad\""
                    "raise", "let value () = raise (System.Exception())"
                    "invalidArg", "let value () = invalidArg \"value\" \"bad\""
                    "alias", "module L = List\nlet value = L.head [1]"
                    "first class function", "let value = List.head<int>"
                ] do
                testCase ("partial function " + name) (fun () ->
                    Expect.isTrue
                        (diagnostics primary source
                         |> List.exists (fun diagnostic ->
                             ([ "AOC004"; "IONIDE-006" ] |> List.contains diagnostic.Code)))
                        name)
            testCase "reverse composition" (fun () ->
                Expect.isTrue
                    (diagnostics primary "let value (x: int) = (id << id) x"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC005"))
                    "composition")
            testCase "comments strings and shadowed names are harmless" (fun () ->
                Expect.isEmpty
                    (diagnostics
                        primary
                        "// mutable <- List.head <|\nlet text = \"mutable <-\"\nmodule List =\n    let head x = x\nlet value = List.head 1")
                    "symbols, not text")
            testCase "factory setter exemption" (fun () ->
                Expect.isEmpty
                    (diagnostics
                        primary
                        "open FParsec\nlet parser, parserRef = createParserForwardedToRef<int, unit>()\nparserRef.Value <- pint32")
                    "FParsec setup")
            testCase "ordinary reference setter is forbidden" (fun () ->
                Expect.isTrue
                    (diagnostics primary "let parserRef = ref 0\nparserRef.Value <- 1"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC002"))
                    "ordinary reference")
            testCase "factory name alone does not grant exemption" (fun () ->
                Expect.isTrue
                    (diagnostics
                        primary
                        "let createParserForwardedToRef () = (), ref 0\nlet parser, parserRef = createParserForwardedToRef()\nparserRef.Value <- 1"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC002"))
                    "resolved FParsec symbol required")
            testCase "factory mentioned inside arbitrary tuple does not grant exemption" (fun () ->
                Expect.isTrue
                    (diagnostics
                        primary
                        "open FParsec\nlet parser, parserRef = (createParserForwardedToRef<int, unit>(), ref 0)\nparserRef.Value <- 1"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC002"))
                    "factory must initialize the binding directly")
            testCase "complex receiver does not inherit exemption" (fun () ->
                Expect.isTrue
                    (diagnostics
                        primary
                        "open FParsec\nlet parser, parserRef = createParserForwardedToRef<int, unit>()\nlet update () = (id parserRef).Value <- pint32"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC002"))
                    "only the direct factory reference setter is exempt")
            testCase "shadowed forwarded name does not inherit exemption" (fun () ->
                Expect.isTrue
                    (diagnostics
                        primary
                        "open FParsec\nlet parser, parserRef = createParserForwardedToRef<int, unit>()\nlet update () =\n    let parserRef = ref 0\n    parserRef.Value <- 1"
                     |> List.exists (fun diagnostic -> diagnostic.Code = "AOC002"))
                    "declaration identity")
            testCase "exploration exemption" (fun () ->
                Expect.isEmpty
                    (diagnostics "/repo/src/Aoc/Y2017/Day01/Mutable.fs" "let mutable count = 0\ncount <- 1")
                    "exploration")
            testCase "shared library is checked" (fun () ->
                Expect.isNonEmpty (diagnostics "/repo/src/Aoc/Shared.fs" "let mutable count = 0") "shared library")
            testCase "app mutation is allowed" (fun () ->
                Expect.isEmpty (diagnostics "/repo/app/Aoc.Cli/Main.fs" "let mutable count = 0\ncount <- 1") "IO edge")
            for fileName in [ "/repo/app/Aoc.Cli/Main.fs"; "/repo/src/Aoc/Y2017/Day01/Mutable.fs" ] do
                testCase ("object rule covers " + fileName) (fun () ->
                    Expect.isNonEmpty
                        (diagnostics fileName "type Counter() = member _.Value = 1")
                        "all production code")
            for name, source in
                [
                    "interface", "type ICounter = abstract Count: int"
                    "object expression", "let value = { new System.IDisposable with member _.Dispose() = () }"
                    "record member", "type Counter =\n    { Value: int }\n    member this.Count = this.Value"
                ] do
                testCase ("object rule " + name) (fun () ->
                    Expect.isTrue
                        (diagnostics primary source
                         |> List.exists (fun diagnostic -> diagnostic.Code = "AOC006"))
                        name)
            testCase "tests are outside production scope" (fun () ->
                Expect.isEmpty
                    (diagnostics "/repo/tests/Example.fs" "type Counter() = member _.Value = 1\nlet mutable count = 0")
                    "test code")
        ]
