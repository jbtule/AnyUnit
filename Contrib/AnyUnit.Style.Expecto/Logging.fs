//
//  Copyright 2026 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

/// A minimal stand-in for Expecto.Logging - the logging facade Expecto's
/// own docs recommend for output from inside a test, and which real
/// suites therefore use (found by porting one: cwtools creates a logger
/// at module scope and routes the library's own log hooks through it).
///
/// Only the surface a test body actually calls: LogLevel, Log.create,
/// logSimple, the info/warn/... members, and Message.event/eventX.
/// Nothing of Expecto's Logary-derived targets, formatting or sinks.
///
/// Where the output goes: into the running test's own ILog when there is
/// one - so it lands in that test's Output in the results, next to its
/// assertions - and to the console otherwise, since creating a logger
/// and logging from module initialisation is legitimate and must not
/// throw.
namespace AnyUnit.Style.Expecto.Logging

open System

type LogLevel =
    | Verbose
    | Debug
    | Info
    | Warn
    | Error
    | Fatal

type Message =
    { level: LogLevel
      template: string
      name: string[] }

/// Not RequireQualifiedAccess, deliberately: real suites write
/// `open Expecto.Logging.Message` and then bare `event`, and that has to
/// keep working under the namespace swap.
module Message =
    /// `Message.event level template` - Expecto's argument order.
    let event (level: LogLevel) (template: string) : Message =
        { level = level; template = template; name = [||] }

    /// `Message.eventX template level` - the flipped order Expecto pairs
    /// with `logger.info (eventX "...")`.
    let eventX (template: string) (level: LogLevel) : Message =
        { level = level; template = template; name = [||] }

type Logger(name: string) =
    let write (message: Message) =
        let line = sprintf "[%A] %s: %s" message.level name message.template
        match AnyUnit.Style.Expecto.Ambient.tryLog () with
        | Some log -> log.WriteLine(line)
        | None -> Console.Error.WriteLine(line)

    member _.Name = name
    member _.logSimple(message: Message) = write message
    /// Expecto's `log`/`logWithAck` are asynchronous; there is nothing to
    /// wait for here, so both complete immediately.
    member _.log (level: LogLevel) (build: LogLevel -> Message) : Async<unit> =
        write (build level)
        async.Return()
    member _.logWithAck (level: LogLevel) (build: LogLevel -> Message) : Async<unit> =
        write (build level)
        async.Return()
    member _.verbose(build: LogLevel -> Message) = write (build Verbose)
    member _.debug(build: LogLevel -> Message) = write (build Debug)
    member _.info(build: LogLevel -> Message) = write (build Info)
    member _.warn(build: LogLevel -> Message) = write (build Warn)
    member _.error(build: LogLevel -> Message) = write (build Error)
    member _.fatal(build: LogLevel -> Message) = write (build Fatal)

[<RequireQualifiedAccess>]
module Log =
    let create (name: string) = Logger(name)
