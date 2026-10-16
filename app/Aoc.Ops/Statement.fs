/// Extracts plain puzzle and submission text from cached HTML values.
module Aoc.Ops.Statement

open System.Net
open System.Text.RegularExpressions

let private replace (pattern: string) (replacement: string) (text: string) =
    Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase ||| RegexOptions.Singleline)

/// Strips tags, preserves block breaks and list markers, and ends with one LF.
let plainText (html: string) : string =
    html
    |> replace @"<li\b[^>]*>" "\n- "
    |> replace @"</?(?:p|pre|li|h2|br|ul)\b[^>]*>" "\n"
    |> replace @"<[^>]+>" ""
    |> WebUtility.HtmlDecode
    |> Option.ofObj
    |> Option.defaultValue ""
    |> fun text -> text.Replace("\r\n", "\n").Replace('\r', '\n')
    |> replace @"\n{3,}" "\n\n"
    |> fun text -> text.Trim() + "\n"

/// Keeps day-desc articles only and removes paragraphs disclosing past answers.
let extract (html: string) : Result<string, string> =
    let articles =
        Regex.Matches(
            html,
            @"<article\b(?=[^>]*\sclass\s*=\s*[""'](?:[^""']*\s)?day-desc(?:\s[^""']*)?[""'])[^>]*>(.*?)</article\s*>",
            RegexOptions.IgnoreCase ||| RegexOptions.Singleline
        )
        |> Seq.map (fun article ->
            Regex.Replace(
                article.Groups[1].Value,
                @"<p\b[^>]*>.*?</p\s*>",
                MatchEvaluator(fun paragraph ->
                    if (plainText paragraph.Value).TrimStart().StartsWith("Your puzzle answer was") then
                        ""
                    else
                        paragraph.Value),
                RegexOptions.IgnoreCase ||| RegexOptions.Singleline
            )
            |> plainText)
        |> Seq.toList

    match articles with
    | [] -> Error "no puzzle articles found"
    | _ ->
        articles
        |> List.map (fun text -> text.TrimEnd())
        |> String.concat "\n\n"
        |> fun text -> Ok(text + "\n")

/// Returns only the article inside main; absent markup is an extraction error.
let submission (html: string) : Result<string, string> =
    let main =
        Regex.Match(html, @"<main\b[^>]*>(.*?)</main\s*>", RegexOptions.IgnoreCase ||| RegexOptions.Singleline)

    let article =
        Regex.Match(
            main.Groups[1].Value,
            @"<article\b[^>]*>(.*?)</article\s*>",
            RegexOptions.IgnoreCase ||| RegexOptions.Singleline
        )

    if article.Success then
        Ok(plainText article.Groups[1].Value)
    else
        Error "no submission article found"
