module Aoc.Tests.OpsTests

open System.IO
open Expecto
open Aoc.Ops
open Aoc.Ops.Bench

let private template name =
    File.ReadAllText(Path.Combine(Repository.root, "templates", name + ".fs.template"))

[<Tests>]
let tests =
    testList
        "Ops"
        [
            testList
                "Doc"
                [
                    let header =
                        "// approach: scan\n// cost: O(n) time and memory, one pass\n// trade: simple\nmodule Example\n"

                    let primary = "src/Aoc/Y2017/Day01/Primary.fs"

                    testCase "command needs no puzzle date" (fun () ->
                        Expect.equal (Command.parse [| "doc" |]) (Ok Command.Doc) "Offline operation")

                    testCase "clean header passes" (fun () ->
                        Expect.equal (Doc.check primary header) [] "Valid ordered header")

                    for name, text, expected in
                        [
                            "missing tag",
                            header.Replace("// trade: simple\n", ""),
                            [ primary + ":3: expected // trade: with non-empty text before module" ]
                            "wrong order",
                            header
                                .Replace("// approach: scan", "// cost: scan")
                                .Replace("// cost: O(n) time and memory, one pass", "// approach: scan"),
                            [
                                primary + ":1: expected // approach: with non-empty text before module"
                                primary + ":2: expected // cost: with non-empty text before module"
                            ]
                            "empty text",
                            header.Replace("// trade: simple", "// trade:   "),
                            [ primary + ":3: expected // trade: with non-empty text before module" ]
                            "header after module",
                            "module Example\n" + header,
                            [
                                primary + ":1: expected // approach: with non-empty text before module"
                                primary + ":2: expected // cost: with non-empty text before module"
                                primary + ":3: expected // trade: with non-empty text before module"
                            ]
                        ] do
                        testCase name (fun () -> Expect.equal (Doc.check primary text) expected name)

                    testCase "Input is exempt" (fun () ->
                        Expect.equal (Doc.check "src/Aoc/Y2017/Day01/Input.fs" "module Example") [] "Domain model")

                    testCase "explorations are checked" (fun () ->
                        Expect.equal
                            (Doc.check
                                "src/Aoc/Y2017/Day01/Recursive.fs"
                                (header.Replace("// trade: simple", "// trade:")))
                            [
                                "src/Aoc/Y2017/Day01/Recursive.fs:3: expected // trade: with non-empty text before module"
                            ]
                            "Same contract")

                    testCase "directives report every source line" (fun () ->
                        Expect.equal
                            (Doc.check
                                "tests/Sample.fs"
                                "// #nowarn harmless\nlet text = \"#nowarn\"\n  #nowarn \"25\"\n#nowarn \"1182\"\n")
                            [
                                "tests/Sample.fs:3: #nowarn is forbidden; document suppressions in Directory.Build.props"
                                "tests/Sample.fs:4: #nowarn is forbidden; document suppressions in Directory.Build.props"
                            ]
                            "Line-start directives only")

                    testCase "lexical multiline limitation is explicit" (fun () ->
                        Expect.equal
                            (Doc.check "app/Sample.fs" "(*\n#nowarn \"25\"\n*)")
                            [
                                "app/Sample.fs:2: #nowarn is forbidden; document suppressions in Directory.Build.props"
                            ]
                            "Documented lexical rule")
                ]
            testList
                "Bench"
                [
                    testCase "commands validate dates" (fun () ->
                        Expect.equal
                            (Command.parse [| "bench"; "2017"; "1" |])
                            (Ok(Command.Bench(2017, 1)))
                            "Bench selection"

                        Expect.equal
                            (Command.parse [| "record"; "2017"; "25" |])
                            (Ok(Command.Record(2017, 25)))
                            "Record selection"

                        Expect.isError (Command.parse [| "bench"; "2017"; "26" |]) "Invalid date")
                    testCase "statistics retain median and range" (fun () ->
                        Expect.equal
                            (Bench.summarize [ 9M; 1M; 5M ])
                            (Some
                                {
                                    Median = 5M
                                    Minimum = 1M
                                    Maximum = 9M
                                })
                            "Odd sample"

                        Expect.equal
                            (Bench.summarize [ 8M; 2M; 6M; 4M ])
                            (Some
                                {
                                    Median = 5M
                                    Minimum = 2M
                                    Maximum = 8M
                                })
                            "Even sample"

                        Expect.equal (Bench.summarize []) None "No fabricated zero")
                    testCase "subtract paired baselines before summarizing" (fun () ->
                        Expect.equal
                            (Bench.work [ 110M, 100M; 140M, 80M; 125M, 120M ])
                            (Some
                                {
                                    Median = 10M
                                    Minimum = 5M
                                    Maximum = 60M
                                })
                            "Pairing matters"

                        Expect.equal
                            (Bench.work [ 9M, 10M ])
                            (Some
                                {
                                    Median = -1M
                                    Minimum = -1M
                                    Maximum = -1M
                                })
                            "Retain noise")
                    testCase "agreement rejects mismatch and failed process without leaking" (fun () ->
                        Expect.equal
                            (Bench.agreement "alternate" "synthetic-oracle" (0, "synthetic-oracle"))
                            (Ok())
                            "Agreement"

                        Expect.equal
                            (Bench.agreement "alternate" "synthetic-oracle" (0, "synthetic-wrong"))
                            (Error "alternate disagrees with primary; answers withheld")
                            "Mismatch"

                        Expect.equal
                            (Bench.agreement "alternate" "synthetic-oracle" (7, "synthetic-oracle"))
                            (Error "alternate process failed (exit 7); diagnostics withheld")
                            "Failure")
                    testCase "record refuses dirty and non-main canned git results" (fun () ->
                        Expect.equal (Bench.canonical (0, "main\n") (0, "")) (Ok()) "Clean main"

                        Expect.isError
                            (Bench.canonical (0, "main") (0, " M app/Aoc.Ops/Program.fs"))
                            "Tracked modification"

                        Expect.isError (Bench.canonical (0, "main") (0, "A  bench/2017.csv")) "Staged modification"
                        Expect.isError (Bench.canonical (0, "work/12345678") (0, "")) "Candidate"
                        Expect.isError (Bench.canonical (0, "") (0, "")) "Detached"
                        Expect.isError (Bench.canonical (1, "main") (0, "")) "Git failure")
                    testCase "CSV quotes text and keeps absent metrics empty" (fun () ->
                        let metadata: Bench.Metadata =
                            {
                                Date = "date"
                                Commit = "revision"
                                Host = "host"
                                Os = "os,arch"
                                Cpu = "a\"b"
                                Compiler = "SDK"
                                Standard = "F# 10"
                            }

                        let summary =
                            {
                                Median = 2.5M
                                Minimum = 1M
                                Maximum = 4M
                            }

                        let measurement: Bench.Measurement =
                            {
                                Solution = "primary"
                                Runs = 10
                                Work = None
                                Instructions = None
                                Wall = summary
                                Cpu = summary
                                Rss = 512M
                            }

                        Expect.equal
                            (Bench.csvRow metadata 2017 1 measurement)
                            "\"date\",\"revision\",\"host\",\"os,arch\",\"a\"\"b\",\"SDK\",\"F# 10\",2017,1,\"primary\",10,,,,,1,2.5,2.5,512"
                            "Shared schema"

                        Expect.equal (Bench.quote "line\none") "\"line\none\"" "Embedded newline"
                        Expect.equal (Bench.number None) "" "Unavailable")
                    testCase "perf parses only usable instruction counts" (fun () ->
                        Expect.equal
                            (Bench.instructions "1200,,instructions:u,1,100.00,,\n0.1,,seconds time elapsed\n")
                            (Some 1200M)
                            "Counter"

                        Expect.equal
                            (Bench.instructions "<not supported>,,instructions:u,0,100.00,,")
                            None
                            "Unsupported"

                        Expect.equal (Bench.instructions "unrelated") None "No counter"

                        Expect.equal
                            (Bench.instructions
                                "1060444,,cpu_atom/instructions/u,765213,100.00,,\n<not counted>,,cpu_core/instructions/u,0,0.00,,\n")
                            (Some 1060444M)
                            "Hybrid CPU reports the selected PMU and an inactive PMU")
                    testCase "GNU time resources use microseconds and KiB" (fun () ->
                        Expect.equal (Bench.resources "0.01,0.02,1234\n") (Ok(30000M, 1234M)) "Units"
                        Expect.isError (Bench.resources "bad,0.02,1234") "Malformed"
                        Expect.isError (Bench.resources "-1,0,1") "Negative")
                ]
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
                    testCase "exploration command" (fun () ->
                        Expect.equal
                            (Command.parse [| "new"; "2017"; "1"; "Recursive" |])
                            (Ok(Command.NewExploration(2017, 1, "Recursive")))
                            "Preserve module casing")
                    for name in [ "Recursive"; "Fold2"; "DFS" ] do
                        testCase ("valid exploration " + name) (fun () ->
                            Expect.equal (Command.explorationName name) (Ok name) "Module identifier")
                    for name in
                        [
                            ""
                            "recursive"
                            "Primary"
                            "Input"
                            "../Escape"
                            "Bad.Name"
                            "Bad-name"
                            "Bad_name"
                            "A\n"
                            "Árbol"
                        ] do
                        testCase (sprintf "invalid exploration %A" name) (fun () ->
                            Expect.isError (Command.parse [| "new"; "2017"; "1"; name |]) "Validate before IO")
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

                    testCase "exploration registry maps lowercase variants" (fun () ->
                        let rendered = Scaffold.yearRegistry (template "Registry") 2017 files

                        Expect.stringContains
                            rendered
                            "Variant = Exploration \"alpha\" }, Day03.Alpha.solve"
                            "Lowercase CLI key"

                        Expect.stringContains
                            rendered
                            "Variant = Exploration \"zebra\" }, Day03.Zebra.solve"
                            "Every exploration registered"

                        Expect.isTrue
                            (rendered.IndexOf("Day03.Primary.solve") < rendered.IndexOf("Day03.Alpha.solve")
                             && rendered.IndexOf("Day03.Alpha.solve") < rendered.IndexOf("Day03.Zebra.solve"))
                            "Primary before sorted explorations")

                    testCase "exploration templates" (fun () ->
                        let solution = Scaffold.exploration 2017 3 "Recursive" (template "Exploration")
                        let tests = Scaffold.exploration 2017 3 "Recursive" (template "ExplorationTests")

                        for tag in [ "// approach:"; "// cost:"; "// trade:" ] do
                            Expect.stringContains solution tag "Required header"

                        Expect.stringContains solution "module Aoc.Y2017.Day03.Recursive" "Module name"
                        Expect.isFalse (solution.Contains "Primary") "Independent solution scaffold"
                        Expect.stringContains tests "recursive agrees with primary" "Variant property"
                        Expect.stringContains tests "FsCheck.FSharp.Prop.forAll" "Generated input comparison")

                    for directory in [ false; true ] do
                        testCase (sprintf "existing exploration path directory=%b" directory) (fun () ->
                            let root =
                                Path.Combine(Path.GetTempPath(), "aoc-scaffold-" + System.Guid.NewGuid().ToString("N"))

                            let day = Path.Combine(root, "src/Aoc/Y2017/Day03")
                            let path = Path.Combine(day, "Recursive.fs")

                            try
                                Directory.CreateDirectory day |> ignore<DirectoryInfo>

                                if directory then
                                    Directory.CreateDirectory path |> ignore<DirectoryInfo>
                                else
                                    File.WriteAllText(path, "preserve")

                                Expect.throwsC
                                    (fun () -> Program.scaffold root 2017 3 (Some "Recursive"))
                                    (fun error ->
                                        Expect.equal
                                            error.Message
                                            "refusing to overwrite an existing exploration path"
                                            "Refuse before writes")

                                if not directory then
                                    Expect.equal (File.ReadAllText path) "preserve" "Existing file intact"
                            finally
                                Directory.Delete(root, true))

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
