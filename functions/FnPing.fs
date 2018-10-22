namespace MyFunctions.Ping

open Chessie.ErrorHandling
open MyFunctions.Types
open MyFunctions.Common
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs.Host
open System.Net
open System.Net.Http

///<summary>
/// This module provides a function to return "Pong!" to the calling client. 
/// It demonstrates a basic GET request and response.
///</summary>
module Get =
    
    let sayPong = trial {
        return "pong!" |> jsonResponse Status.OK 
    }

    let workflow (req: HttpRequest) = asyncTrial {
        let! result = sayPong
        return result
    }

    /// <summary>
    /// Say hello to a person by name.
    /// </summary>
    let run (req: HttpRequest) (log: TraceWriter) = async {
        let! result = (workflow req) |> Async.ofAsyncResult
        return constructResponse log result
    }