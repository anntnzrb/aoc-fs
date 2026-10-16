module Aoc.Analyzers.Rules

open System
open System.IO
open Microsoft.FSharp.Reflection
open FSharp.Analyzers.SDK
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

// Traverse syntax data without a mutable visitor or handwritten child enumeration.
[<TailCall>]
let rec private nodes (value: objnull) =
    seq {
        match Option.ofObj value with
        | None -> ()
        | Some value ->
            let kind = value.GetType()
            let syntax = kind.Namespace = typeof<SynExpr>.Namespace

            let container =
                kind.IsGenericType
                && [ typedefof<obj list>; typedefof<obj option> ]
                   |> List.contains (
                       if kind.IsGenericType then
                           kind.GetGenericTypeDefinition()
                       else
                           kind
                   )

            if syntax || container || FSharpType.IsTuple kind then
                yield value

                let children =
                    if FSharpType.IsUnion kind then
                        FSharpValue.GetUnionFields(value, kind) |> snd
                    elif FSharpType.IsRecord kind then
                        FSharpValue.GetRecordFields value
                    elif FSharpType.IsTuple kind then
                        FSharpValue.GetTupleFields value
                    else
                        [||]

                for child in children do
                    yield! nodes child
    }

let private contains (outer: range) (inner: range) =
    outer.FileName = inner.FileName
    && (outer.StartLine, outer.StartColumn) <= (inner.StartLine, inner.StartColumn)
    && (inner.EndLine, inner.EndColumn) <= (outer.EndLine, outer.EndColumn)

let private message code text range =
    {
        Type = "Repository discipline"
        Message = text
        Code = code
        Severity = Severity.Warning
        Range = range
        Fixes = []
    }

let private sourceScope (projectFileName: string) (fileName: string) =
    // All registered projects live two directories below the repository root.
    let projectDirectory =
        Path.GetDirectoryName projectFileName |> Option.ofObj |> Option.defaultValue "."

    let root = Path.GetFullPath("../..", projectDirectory)
    let path = "/" + (Path.GetRelativePath(root, fileName)).Replace('\\', '/')
    let library = path.StartsWith("/src/Aoc/", StringComparison.Ordinal)

    let production =
        path.StartsWith("/src/", StringComparison.Ordinal)
        || path.StartsWith("/app/", StringComparison.Ordinal)

    let segments = path.Split('/') |> Array.toList |> List.rev

    let exploration =
        match segments with
        | name :: parent :: _ ->
            parent.Length = 5
            && parent.StartsWith("Day", StringComparison.Ordinal)
            && (parent[3..] |> Seq.forall Char.IsAsciiDigit)
            && not ([ "Input.fs"; "Primary.fs" ] |> List.contains name)
        | _ -> false

    library && not exploration, production

let private collection (entity: FSharpEntity) =
    entity.Namespace = Some "System.Collections.Generic"
    && [ "Dictionary"; "HashSet"; "List"; "Queue"; "Stack" ]
       |> List.contains entity.DisplayName

[<TailCall>]
let rec private bindingPattern =
    function
    | SynPat.Paren(pat = pattern)
    | SynPat.Typed(pat = pattern) -> bindingPattern pattern
    | pattern -> pattern

[<TailCall>]
let rec private calledFunction =
    function
    | SynExpr.App(funcExpr = expression)
    | SynExpr.TypeApp(expr = expression)
    | SynExpr.Paren(expr = expression)
    | SynExpr.Typed(expr = expression) -> calledFunction expression
    | SynExpr.Ident identifier -> Some identifier.idRange
    | SynExpr.LongIdent(range = range) -> Some range
    | _ -> None

[<TailCall>]
let rec private publicEntity (entity: FSharpEntity) =
    entity.Accessibility.IsPublic
    && match entity.DeclaringEntity with
       | Some parent -> publicEntity parent
       | None -> true

let private undocumented =
    function
    | FSharpXmlDoc.None -> true
    | FSharpXmlDoc.FromXmlText text -> text.IsEmpty
    | FSharpXmlDoc.FromXmlFile _ -> false

let private documentation (uses: FSharpSymbolUse list) =
    uses
    |> List.choose (fun usage ->
        let missing =
            if not usage.IsFromDefinition then
                false
            else
                match usage.Symbol with
                | :? FSharpEntity as entity ->
                    not entity.IsFSharpModule
                    && not entity.IsNamespace
                    && publicEntity entity
                    && undocumented entity.XmlDoc
                | :? FSharpMemberOrFunctionOrValue as value ->
                    value.IsModuleValueOrMember
                    && not value.IsMember
                    && not value.IsCompilerGenerated
                    && value.Accessibility.IsPublic
                    && (value.DeclaringEntity |> Option.exists publicEntity)
                    && undocumented value.XmlDoc
                | :? FSharpUnionCase as unionCase ->
                    unionCase.Accessibility.IsPublic
                    && publicEntity unionCase.DeclaringEntity
                    && unionCase.DeclaringEntity.RepresentationAccessibility.IsPublic
                    && undocumented unionCase.XmlDoc
                | :? FSharpField as field ->
                    not field.IsUnionCaseField
                    && not field.IsCompilerGenerated
                    && field.Accessibility.IsPublic
                    && (field.DeclaringEntity
                        |> Option.exists (fun entity ->
                            publicEntity entity && entity.RepresentationAccessibility.IsPublic))
                    && undocumented field.XmlDoc
                | _ -> false

        if missing then
            Some(message "AOC007" "Public library declarations require a /// XML doc comment." usage.Range)
        else
            None)

let private forwardedReceiver (syntax: obj list) (uses: FSharpSymbolUse list) =
    let forwarded =
        syntax
        |> List.choose (function
            | :? SynBinding as binding ->
                let (SynBinding(headPat = pattern; expr = expression)) = binding

                match bindingPattern pattern with
                | SynPat.Tuple(elementPats = [ _; SynPat.Named(ident = SynIdent(reference, _)) ]) when
                    uses
                    |> List.exists (fun usage ->
                        (calledFunction expression
                         |> Option.exists (fun range -> contains range usage.Range))
                        && match usage.Symbol with
                           | :? FSharpMemberOrFunctionOrValue as symbol ->
                               symbol.FullName = "FParsec.Primitives.createParserForwardedToRef"
                           | _ -> false)
                    ->
                    Some reference.idRange
                | _ -> None
            | _ -> None)

    let forwardedReceiver (receiverRange: range) =
        uses
        |> List.exists (fun usage ->
            receiverRange = usage.Range
            && match usage.Symbol.DeclarationLocation with
               | Some declaration -> forwarded |> List.contains declaration
               | None -> false)

    forwardedReceiver

let private syntaxMessages solution forwardedReceiver (syntax: obj list) =
    syntax
    |> List.collect (function
        | :? SynBinding as binding when solution ->
            let (SynBinding(isMutable = mutableBinding; headPat = pattern)) = binding

            if mutableBinding then
                [
                    message "AOC001" "Mutable bindings are forbidden in solutions." pattern.Range
                ]
            else
                []
        | :? SynField as field when solution ->
            let (SynField(isMutable = mutableField; range = range)) = field

            if mutableField then
                [ message "AOC001" "Mutable fields are forbidden in solutions." range ]
            else
                []
        | :? SynExpr as expression ->
            match expression with
            | SynExpr.ObjExpr(range = range) -> [ message "AOC006" "Object expressions are forbidden." range ]
            | SynExpr.LongIdentSet(longDotId = SynLongIdent(id = [ receiver; property ])) when
                solution && property.idText = "Value" && forwardedReceiver receiver.idRange
                ->
                []
            | SynExpr.DotSet(targetExpr = receiver; longDotId = SynLongIdent(id = [ property ])) when
                solution && property.idText = "Value" && forwardedReceiver receiver.Range
                ->
                []
            | SynExpr.LongIdentSet _
            | SynExpr.DotSet _
            | SynExpr.Set _
            | SynExpr.DotIndexedSet _
            | SynExpr.NamedIndexedPropertySet _
            | SynExpr.DotNamedIndexedPropertySet _ when solution ->
                [ message "AOC002" "Assignment is forbidden in solutions." expression.Range ]
            | _ -> []
        | :? SynTypeDefnRepr as representation ->
            match representation with
            | SynTypeDefnRepr.ObjectModel(range = range) ->
                [
                    message "AOC006" "Classes, interfaces and type extensions are forbidden." range
                ]
            | SynTypeDefnRepr.Simple(simpleRepr = SynTypeDefnSimpleRepr.General(range = range)) ->
                [ message "AOC006" "Object models are forbidden." range ]
            | _ -> []
        | :? SynMemberDefn as memberDefinition ->
            match memberDefinition with
            | SynMemberDefn.Member(range = range)
            | SynMemberDefn.GetSetMember(range = range)
            | SynMemberDefn.AbstractSlot(range = range)
            | SynMemberDefn.AutoProperty(range = range)
            | SynMemberDefn.Interface(range = range)
            | SynMemberDefn.Inherit(range = range)
            | SynMemberDefn.ImplicitInherit(range = range) ->
                [ message "AOC006" "Authored members and inheritance are forbidden." range ]
            | _ -> []
        | _ -> [])


let private symbolMessages solution (uses: FSharpSymbolUse list) =
    if not solution then
        []
    else
        uses
        |> List.choose (fun usage ->
            if usage.IsFromDefinition then
                None
            else
                match usage.Symbol with
                | :? FSharpEntity as entity when collection entity ->
                    Some(message "AOC003" "Mutable collections are forbidden in solutions." usage.Range)
                | :? FSharpMemberOrFunctionOrValue as symbol ->
                    let name = symbol.FullName

                    let owner =
                        symbol.DeclaringEntity
                        |> Option.map (fun entity -> entity.FullName)
                        |> Option.defaultValue ""

                    if symbol.DeclaringEntity |> Option.exists collection then
                        Some(message "AOC003" "Mutable collections are forbidden in solutions." usage.Range)
                    elif
                        [
                            "Microsoft.FSharp.Collections.ArrayModule"
                            "Microsoft.FSharp.Collections.Array2DModule"
                        ]
                        |> List.contains owner
                        && [ "Set"; "Fill"; "Blit"; "CopyTo"; "ZeroCreate" ]
                           |> List.contains symbol.CompiledName
                    then
                        Some(message "AOC003" "In-place array operations and zeroCreate are forbidden." usage.Range)
                    elif
                        [
                            "Microsoft.FSharp.Collections.List.head"
                            "Microsoft.FSharp.Collections.Seq.head"
                            "Microsoft.FSharp.Collections.List.item"
                            "Microsoft.FSharp.Core.Operators.failwith"
                            "Microsoft.FSharp.Core.Operators.raise"
                            "Microsoft.FSharp.Core.Operators.invalidArg"
                        ]
                        |> List.contains name
                    then
                        Some(message "AOC004" "Partial functions are forbidden; return Option or Result." usage.Range)
                    elif
                        owner = "Microsoft.FSharp.Core.Operators"
                        && [ "op_PipeLeft"; "op_ComposeLeft" ] |> List.contains symbol.CompiledName
                    then
                        Some(message "AOC005" "Use forward pipelines and composition." usage.Range)
                    else
                        None
                | _ -> None)


/// Enforce functional solution rules and forbid authored object models in production files.
/// Exploration modules are exempt from solution rules, but not object-model rules.
[<CliAnalyzer("Aoc.Discipline", "Enforce repository functional programming rules")>]
let analyze: Analyzer<CliContext> =
    fun context ->
        async {
            let solution, production =
                sourceScope context.ProjectOptions.ProjectFileName context.FileName

            if not production then
                return []
            else
                let syntax = nodes (box context.ParseFileResults.ParseTree) |> Seq.toList
                let uses = context.GetAllSymbolUsesOfFile() |> Seq.toList
                let normalized = context.FileName.Replace('\\', '/')

                let libraryDocumentation =
                    normalized.Contains("/src/Aoc/", StringComparison.Ordinal)
                    && not (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"/Y[0-9]{4}/Day[0-9]{2}/"))

                let documentationMessages = if libraryDocumentation then documentation uses else []
                // Only the second tuple binding of FParsec's real factory may be assigned.
                // Match the reference's declaration identity, so shadowed lookalikes are rejected.
                let syntaxMessages = syntaxMessages solution (forwardedReceiver syntax uses) syntax
                let symbolMessages = symbolMessages solution uses

                let! optionMessages =
                    if solution then
                        Ionide.Analyzers.Suggestion.HandleOptionGracefullyAnalyzer.optionGetCliAnalyzer context
                    else
                        async.Return []

                return
                    syntaxMessages @ symbolMessages @ optionMessages @ documentationMessages
                    |> List.distinctBy (fun diagnostic -> diagnostic.Code, diagnostic.Range)
        }
