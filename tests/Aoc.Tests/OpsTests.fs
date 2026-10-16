module Aoc.Tests.OpsTests

open System.IO
open Expecto
open Aoc.Ops

let private template name =
    File.ReadAllText(Path.Combine(Repository.root, "templates", name + ".fs.template"))

[<Tests>]
let tests =
    testList
        "Ops"
        [
            testCase "User-Agent is a valid header value" (fun () ->
                use message = new System.Net.Http.HttpRequestMessage()
                Expect.isTrue (message.Headers.UserAgent.TryParseAdd Program.userAgent) "Header accepts the value")
            testList
                "Session"
                [
                    for name, text, expected in
                        [
                            "present", "OTHER=x\nAOC_SESSION=fake\n", Ok "fake"
                            "missing", "OTHER=x", Error "AOC_SESSION not set in .env"
                            "double quoted", " AOC_SESSION = \"fake=value\" ", Ok "fake=value"
                            "single quoted", "AOC_SESSION='fake'", Ok "fake"
                            "commented", "# AOC_SESSION=hidden\n\nAOC_SESSION=visible", Ok "visible"
                            "only comments", " # AOC_SESSION=hidden\n", Error "AOC_SESSION not set in .env"
                            "empty", "AOC_SESSION=\"\"", Error "AOC_SESSION not set in .env"
                            "hex token with CRLF", "AOC_SESSION=0123456789abcdefABCDEF\r\n", Ok "0123456789abcdefABCDEF"
                            "every cookie octet",
                            "AOC_SESSION=!#$%&'()*+-./0123456789:<=>?@AZ[]^_`az{|}~",
                            Ok "!#$%&'()*+-./0123456789:<=>?@AZ[]^_`az{|}~"
                        ] do
                        testCase name (fun () -> Expect.equal (Session.parse text) expected "Configuration parse")
                    for name, token in
                        [
                            "CR", "synthetic\rTOKEN"
                            "LF", "synthetic\nTOKEN"
                            "NUL", "synthetic\000TOKEN"
                            "space", "synthetic TOKEN"
                            "semicolon", "synthetic;TOKEN"
                        ] do
                        testCase ("invalid cookie " + name) (fun () ->
                            match Session.parse ("AOC_SESSION=\"" + token + "\"") with
                            | Ok _ -> failtest "Invalid cookie value accepted"
                            | Error message ->
                                Expect.equal
                                    message
                                    "AOC_SESSION in .env is not a valid cookie value"
                                    "Fixed diagnostic"

                                Expect.isFalse (message.Contains token) "Synthetic token must never appear in errors")
                ]
            testList
                "Submission"
                [
                    for text, expected in
                        [
                            "That's the right answer!", 0
                            "That's not the right answer.", 1
                            "You gave an answer too recently. You have 12s left to wait.", 2
                            "You don't seem to be solving the right level.", 2
                            "Unexpected response", 3
                        ] do
                        testCase text (fun () -> Expect.equal (Submission.classify text) expected "Server verdict")
                    for answer in [ "42"; "abcXYZ"; "a,b-c"; "-123" ] do
                        testCase ("valid " + answer) (fun () ->
                            Expect.equal (Submission.validate "1" answer) (Ok(1, answer)) "Accepted token")
                    for answer in [ ""; "bad answer"; "42\n"; "é"; "１"; "a=b" ] do
                        testCase (sprintf "invalid %A" answer) (fun () ->
                            Expect.isError (Submission.validate "1" answer) "Invalid token")
                    testCase "part 2 accepted" (fun () ->
                        Expect.equal (Submission.validate "2" "42") (Ok(2, "42")) "Second part")
                    for part in [ "0"; "3"; "01"; "" ] do
                        testCase ("invalid part " + part) (fun () ->
                            Expect.isError (Submission.validate part "42") "Invalid part")
                ]
            testList
                "Command"
                [
                    testCase "fetch cache default" (fun () ->
                        Expect.equal
                            (Command.parse [| "fetch"; "2017"; "1" |])
                            (Ok(Command.Fetch(2017, 1, false)))
                            "Cache policy")
                    testCase "fetch force" (fun () ->
                        Expect.equal
                            (Command.parse [| "fetch"; "2017"; "1"; "--force" |])
                            (Ok(Command.Fetch(2017, 1, true)))
                            "Force policy")
                    testCase "invalid date" (fun () ->
                        Expect.isError (Command.parse [| "fetch"; "2017"; "26" |]) "Calendar validation")
                    testCase "invalid submission" (fun () ->
                        Expect.isError
                            (Command.parse [| "submit"; "2017"; "1"; "1"; "bad answer" |])
                            "Validate before IO")
                    testCase "exploration refusal" (fun () ->
                        Expect.equal
                            (Command.parse [| "new"; "2017"; "1"; "Recursive" |])
                            (Error "explorations are not supported yet")
                            "Primary only")
                ]
            testList
                "Statement"
                [
                    testCase "two articles, blocks, lists, entities and past answers" (fun () ->
                        let html =
                            """<html><nav>ignore</nav><main><article class="day-desc"><h2>Title &amp; more</h2><p>Example &lt;x&gt;.</p><pre><code>a
b</code></pre><ul><li>one</li><li>two &quot;quoted&quot;</li></ul><p>Your <em>puzzle answer</em> was <code>SECRET</code>.</p></article><p>Your puzzle answer was OUTSIDE.</p><article class='day-desc'><h2>Part Two</h2><p>Next<br>line.</p></article><aside>ignore</aside></main></html>"""

                        Expect.equal
                            (Statement.extract html)
                            (Ok
                                "Title & more\n\nExample <x>.\n\na\nb\n\n- one\n\n- two \"quoted\"\n\nPart Two\n\nNext\nline.\n")
                            "Only puzzle articles remain")
                    testCase "no article" (fun () ->
                        Expect.isError (Statement.extract "<main>empty</main>") "Missing articles fail")
                    testCase "class must be a complete token" (fun () ->
                        Expect.isError
                            (Statement.extract "<article class='not-day-desc'>excluded</article>")
                            "Reject class substrings"

                        Expect.isError
                            (Statement.extract "<article data-class='day-desc'>excluded</article>")
                            "Require class attribute"

                        Expect.equal
                            (Statement.extract "<article class='extra day-desc other'><p>kept</p></article>")
                            (Ok "kept\n")
                            "Accept class lists")
                    testCase "code indentation retained" (fun () ->
                        Expect.equal
                            (Statement.extract
                                "<article class='day-desc'><pre><code>first\n  second</code></pre></article>")
                            (Ok "first\n  second\n")
                            "Preserve example whitespace")
                    testCase "submission main article only" (fun () ->
                        Expect.equal
                            (Statement.submission
                                "<article>outside</article><main><article><p>You have 5s left to wait.</p></article></main>")
                            (Ok "You have 5s left to wait.\n")
                            "Preserve verdict text")
                    testCase "submission no article" (fun () ->
                        Expect.isError (Statement.submission "<main>empty</main>") "Missing verdict fails")
                ]
            testList
                "Scaffold"
                [
                    let files =
                        [
                            "Y2017/Day10/Primary.fs"
                            "Y2017/Day10/Input.fs"
                            "Y2016/Day03/Primary.fs"
                            "Y2016/Day03/Input.fs"
                            "Y2017/Day01/Primary.fs"
                            "Y2017/Day01/Input.fs"
                            "Y2017/Day03/Primary.fs"
                            "Y2017/Day03/Input.fs"
                            "Y2017/Day03/Zebra.fs"
                            "Y2017/Day03/Alpha.fs"
                            "Y2017/Registry.fs"
                            "Y2016/Registry.fs"
                        ]

                    testCase "library ordering" (fun () ->
                        Expect.equal
                            (Scaffold.libraryEntries files)
                            [
                                "Y2016/Day03/Input.fs"
                                "Y2016/Day03/Primary.fs"
                                "Y2016/Registry.fs"
                                "Y2017/Day01/Input.fs"
                                "Y2017/Day01/Primary.fs"
                                "Y2017/Day03/Input.fs"
                                "Y2017/Day03/Primary.fs"
                                "Y2017/Day03/Alpha.fs"
                                "Y2017/Day03/Zebra.fs"
                                "Y2017/Day10/Input.fs"
                                "Y2017/Day10/Primary.fs"
                                "Y2017/Registry.fs"
                            ]
                            "Dependency order")

                    testCase "test ordering" (fun () ->
                        Expect.equal
                            (Scaffold.testEntries
                                [
                                    "OpsTests.fs"
                                    "Y2017/Day10Tests.fs"
                                    "Y2016/Day03Tests.fs"
                                    "Y2017/Day03Tests.fs"
                                    "Y2017/Day01Tests.fs"
                                ])
                            [
                                "Y2016/Day03Tests.fs"
                                "Y2017/Day01Tests.fs"
                                "Y2017/Day03Tests.fs"
                                "Y2017/Day10Tests.fs"
                            ]
                            "Chronological tests")

                    testCase "empty registries exact" (fun () ->
                        let year =
                            "// Generated by `aoc-ops new`; do not edit.\n/// Registered implementations for 2017.\nmodule Aoc.Y2017.Registry\n\nopen Aoc.Solution\n\n/// Maps puzzle keys to pure solvers for this year.\nlet solvers: Map<Key, Solver> = Map.empty\n"

                        let root =
                            "// Generated by `aoc-ops new`; do not edit.\n/// All registered puzzle implementations, merged from the year registries.\nmodule Aoc.Registry\n\nopen Aoc.Solution\n\n/// Maps each registered year, day and variant to its pure solver.\nlet solvers: Map<Key, Solver> =\n    [ Y2017.Registry.solvers ] |> Seq.collect Map.toSeq |> Map.ofSeq\n"

                        Expect.equal
                            (Scaffold.yearRegistry (template "Registry") 2017 [ "Y2017/Registry.fs" ])
                            year
                            "Year registry baseline"

                        Expect.equal
                            (Scaffold.rootRegistry (template "RootRegistry") [ "Y2017/Registry.fs" ])
                            root
                            "Root registry baseline")

                    testCase "root registry ordering" (fun () ->
                        let rendered = Scaffold.rootRegistry (template "RootRegistry") files

                        Expect.stringContains
                            rendered
                            "[ Y2016.Registry.solvers; Y2017.Registry.solvers ]"
                            "Ascending years")

                    testCase "year registry days" (fun () ->
                        let rendered = Scaffold.yearRegistry (template "Registry") 2017 files

                        Expect.stringContains
                            rendered
                            "Year = 2017; Day = 1; Variant = Primary }, Day01.Primary.solve"
                            "Primary entry"

                        Expect.isTrue
                            (rendered.IndexOf("Day01.Primary.solve") < rendered.IndexOf("Day03.Primary.solve")
                             && rendered.IndexOf("Day03.Primary.solve") < rendered.IndexOf("Day10.Primary.solve"))
                            "Ascending days")

                    testCase "project XML preserved and repeatable" (fun () ->
                        let original =
                            "<Project>\n  <ItemGroup>\n    <Compile Include=\"Calendar.fs\" />\n    <!-- keep before Y -->\n    <Compile Include=\"Y2017/Registry.fs\" />\n    <Compile Include=\"Registry.fs\" />\n  </ItemGroup>\n  <!-- unrelated -->\n  <ItemGroup><PackageReference Include=\"FSharp.Core\" /></ItemGroup>\n</Project>\n"

                        let entries = Scaffold.libraryEntries files

                        match Scaffold.project true entries original with
                        | Error message -> failtest message
                        | Ok generated ->
                            Expect.stringContains
                                generated
                                "<!-- keep before Y -->\n    <Compile Include=\"Y2016/Day03/Input.fs\" />"
                                "Keep comment before generated block"

                            Expect.stringContains
                                generated
                                "<!-- unrelated -->\n  <ItemGroup><PackageReference Include=\"FSharp.Core\" /></ItemGroup>"
                                "Unrelated XML intact"

                            Expect.equal
                                (Scaffold.project true entries generated)
                                (Ok generated)
                                "Repeated regeneration")

                    testCase "test project keeps non-Y entries first" (fun () ->
                        let original =
                            "<ItemGroup>\n  <Compile Include=\"OpsTests.fs\" />\n  <!-- day tests -->\n</ItemGroup>\n"

                        let expected =
                            "<ItemGroup>\n  <Compile Include=\"OpsTests.fs\" />\n  <Compile Include=\"Y2017/Day01Tests.fs\" />\n  <!-- day tests -->\n</ItemGroup>\n"

                        Expect.equal
                            (Scaffold.project false [ "Y2017/Day01Tests.fs" ] original)
                            (Ok expected)
                            "Append after non-Y Compile")

                    testCase "missing anchor fails" (fun () ->
                        Expect.isError (Scaffold.project true [] "<Project />") "Refuse malformed project")

                    for name, line in
                        [
                            "inline comment", "  <Compile Include=\"Y2017/Registry.fs\" /> <!-- comment -->"
                            "single quoted attribute", "  <Compile Include='Y2017/Registry.fs' />"
                        ] do
                        testCase ("unsupported Compile " + name) (fun () ->
                            for library, path in
                                [ true, "src/Aoc/Aoc.fsproj"; false, "tests/Aoc.Tests/Aoc.Tests.fsproj" ] do
                                Expect.equal
                                    (Scaffold.project library [] ("<Project>\n" + line + "\n</Project>\n"))
                                    (Error(sprintf "unsupported Compile line in %s: %s" path (line.Trim())))
                                    "Refuse before regeneration")

                    testCase "current project shape idempotent" (fun () ->
                        for library, path, directory in
                            [
                                true, "src/Aoc/Aoc.fsproj", "src/Aoc"
                                false, "tests/Aoc.Tests/Aoc.Tests.fsproj", "tests/Aoc.Tests"
                            ] do
                            let text = File.ReadAllText(Path.Combine(Repository.root, path))
                            let basePath = Path.Combine(Repository.root, directory)

                            let paths =
                                Directory.GetFiles(basePath, "*.fs", SearchOption.AllDirectories)
                                |> Array.map (fun path -> Path.GetRelativePath(basePath, path).Replace('\\', '/'))
                                |> Array.toList

                            let entries =
                                if library then
                                    Scaffold.libraryEntries paths
                                else
                                    Scaffold.testEntries paths

                            Expect.equal
                                (Scaffold.project library entries text)
                                (Ok text)
                                "Current project reproduced exactly")
                ]
        ]
