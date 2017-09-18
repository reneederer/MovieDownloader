namespace Episodes

open Aether
open Aether.Operators
open System
open System.Collections.Generic
open System.Linq
open System.Text
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Net.Http
open System.Net
open System.IO
open HtmlAgilityPack
open FSharp.Data
open System.Text.RegularExpressions
open FSharpx
open FSharpx.Option
open System.Runtime.Serialization.Json
open Logger.Logger
open Logger
open System.Diagnostics
open Java.Lang
open Java.Interop
open System.Threading
open System.Threading.Tasks

module EpisodeTypes =
    type EpisodeDownload =
        { targetDir : string
        ; fileName : string
        ; args : list<string>
        ; hosterUrls : list<string>
        ; activeHosterIndex : int
        }
