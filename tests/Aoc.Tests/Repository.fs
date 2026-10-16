module Aoc.Tests.Repository

open System
open System.IO

let root: string =
    let rec findRoot (directory: DirectoryInfo) =
        if File.Exists(Path.Combine(directory.FullName, "global.json")) then
            Some directory.FullName
        else
            match directory.Parent with
            | null -> None
            | parent -> findRoot parent

    match findRoot (DirectoryInfo AppContext.BaseDirectory) with
    | Some root -> root
    | None ->
        match findRoot (DirectoryInfo(Directory.GetCurrentDirectory())) with
        | Some root -> root
        | None -> failwith "Cannot find repository root: no global.json above the test directory or current directory."
