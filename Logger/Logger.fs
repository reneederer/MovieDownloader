namespace Logger

open System

module Logger =
    type LogEvent =
        | Info of string
        | Debug of string
        | Warning of string
        | Error of string
    type Logger () = 
        member val observers = [] with get, set
        member this.addObserver (observer : Action<LogEvent>) =
            this.observers <- observer :: this.observers
        member this.log logEvent =
            for observer in this.observers do
                observer.Invoke(logEvent)
    let logger = new Logger()
    let toString logEvent =
        match logEvent with
        | Info s -> "Info\t" + s
        | Debug s -> "Debug\t" + s
        | Warning s -> "Warning\t" + s
        | Error s -> "Error\t" + s



