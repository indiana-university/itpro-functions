// Copyright (C) 2018 The Trustees of Indiana University
// SPDX-License-Identifier: BSD-3-Clause    

namespace Tasks 

module Types=
    open Core.Types

    type IDataRepository =
      { GetAllNetIds: unit -> Async<Result<seq<NetId>, Error>>
        FetchLatestHRPerson: NetId -> Async<Result<Person option, Error>>
        UpdatePerson: Person -> Async<Result<Person, Error>>
        GetAllTools: unit -> Async<Result<seq<Tool>, Error>>
        GetADGroupMembers: string -> Async<Result<seq<NetId>, Error>>
        GetAllToolUsers: Tool -> Async<Result<seq<NetId>, Error>> }
    
    type ADPath = string

    type ToolPersonUpdate =
    | Add of NetId * ADPath
    | Remove of NetId * ADPath


module FakeRepository=
    open Types
    open Core.Types
    open Core.Fakes

    let Respository = 
     { GetAllNetIds = fun () -> ["rswanso"] |> List.toSeq |> ok
       FetchLatestHRPerson = fun _ -> Some(swanson) |> ok
       UpdatePerson = fun person -> person |> ok
       GetAllTools = fun () -> [tool] |> List.toSeq |> ok
       GetADGroupMembers = fun _ -> ["rswanso"] |> List.toSeq |> ok
       GetAllToolUsers = fun _ -> ["lknope"] |> List.toSeq |> ok }

module DataRepository =
    open Types
    open Core.Types
    open Core.Util
    open Database.Command
    open Dapper
    open System.Net.Http
    open System.Net.Http.Headers

    let getAllNetIds connStr =
        let sql = "SELECT netid FROM people;"
        let queryFn (cn:Cn) = cn.QueryAsync<NetId>(sql)
        fetch connStr queryFn

    let fetchLatestHrPerson sharedSecret netid =
        let url = sprintf "https://itpeople-adapter.apps.iu.edu/people/%s" netid
        let msg = new HttpRequestMessage(HttpMethod.Get, url)      
        msg.Headers.Authorization <-  AuthenticationHeaderValue("Bearer", sharedSecret)
        sendAsync<seq<Person>> msg
        >>= (Seq.tryFind (fun p -> p.NetId=netid) >> ok)

    let updatePerson connStr (person:Person) = 
        let sql = """
        UPDATE people 
        SET name = @Name,
            position = @Position,
            campus = @Campus,
            campus_phone = @CampusPhone,
            campus_email = @CampusEmail
        WHERE netid = @NetId
        RETURNING *;"""
        let queryFn (cn:Cn) = cn.QuerySingleAsync<Person>(sql, person)
        fetch connStr queryFn

    let Repository connStr sharedSecret =
     { GetAllNetIds = fun () -> getAllNetIds connStr
       FetchLatestHRPerson = fetchLatestHrPerson sharedSecret
       UpdatePerson = updatePerson connStr
       GetAllTools = fun () -> System.NotImplementedException() |> raise 
       GetADGroupMembers = fun str -> System.NotImplementedException() |> raise 
       GetAllToolUsers = fun tool -> System.NotImplementedException() |> raise }

module Functions=

    open Core.Types
    open Core.Json

    open System
    open System.Net
    open System.Net.Http
    open Microsoft.Azure.WebJobs
    open Microsoft.Azure.WebJobs.Extensions.Http
    open Microsoft.Extensions.Logging

    open Types
    open Newtonsoft.Json

    let execute (workflow:'a -> Async<Result<'b,Error>>) (arg:'a)= 
        async {
            let! result = workflow arg
            match result with
            | Ok(_) -> ()
            | Error(msg) -> 
                msg
                |> sprintf "Workflow failed with error: %A"
                |> System.Exception
                |> raise
        } |> Async.RunSynchronously

    let data = 
        let connStr = Environment.GetEnvironmentVariable("DbConnectionString")
        let sharedSecret = Environment.GetEnvironmentVariable("SharedSecret")
        DataRepository.Repository connStr sharedSecret

    /// This module defines the bindings and triggers for all functions in the project
    [<FunctionName("PingGet")>]
    let ping
        ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")>] req:HttpRequestMessage) =
        req.CreateResponse(HttpStatusCode.OK, "pong!")

    // Enqueue the netids of all the people for whom we need to update
    // canonical HR data.
    [<FunctionName("PeopleUpdateBatcher")>]
    let peopleUpdateBatcher
        ([<TimerTrigger("0 0 14 * * *", RunOnStartup=true)>] timer: TimerInfo,
         [<Queue("people-update")>] queue: ICollector<string>,
         log: ILogger) = 
        
        let enqueueAllNetIds =
            Seq.iter queue.Add

        let logEnqueuedNumber = 
            Seq.length
            >> sprintf "Enqueued %d netids for update."
            >> log.LogInformation

        let workflow = 
            data.GetAllNetIds
            >=> tap enqueueAllNetIds
            >=> tap logEnqueuedNumber

        execute workflow ()

    // Pluck a netid from the queue, fetch that person's HR data from the API, 
    // and update it in the DB.
    [<FunctionName("PeopleUpdateWorker")>]
    let peopleUpdateWorker
        ([<QueueTrigger("people-update")>] netid: string,
         log: ILogger) =

        let logUpdatedPerson (person:Person) = 
            person.NetId
            |> sprintf "Updated HR data for %s."
            |> log.LogInformation
            ok ()

        let logPersonNotFound () = 
            netid
            |> sprintf "HR data not found for %s."
            |> log.LogWarning
            ok ()

        let processHRResult result =
            match result with
            | Some(person) ->
                data.UpdatePerson person
                >>= logUpdatedPerson
            | None ->
                logPersonNotFound ()

        let workflow = 
            data.FetchLatestHRPerson
            >=> processHRResult

        execute workflow netid

    let enqueueAll (queue:ICollector<string>) =
        Seq.map serialize
        >> Seq.iter queue.Add

        // Enqueue the tools for which permissions need to be updated.
    [<FunctionName("ToolUpdateBatcher")>]
    let toolUpdateBatcher
        ([<TimerTrigger("0 0 14 * * *", RunOnStartup=true)>] timer: TimerInfo,
         [<Queue("tool-update")>] queue: ICollector<string>,
         log: ILogger) =

         let logEnqueuedTools = 
            Seq.map (fun t -> t.Name)
            >> String.concat ", "
            >> sprintf "Enqueued tool permission updates for: %s"
            >> log.LogInformation

         let workfow =
            data.GetAllTools
            >=> tap (enqueueAll queue)
            >=> tap logEnqueuedTools
         
         execute workfow ()

    // Pluck a tool from the queue. 
    // Fetch all the people that should have access to this tool, then fetch 
    // all the people currently in the AD group associated with this tool. 
    // Determine which people should be added/removed from that AD group
    // and enqueue and add/remove message for each.
    [<FunctionName("ToolUpdateWorker")>]
    let toolUpdateWorker
        ([<QueueTrigger("tool-update")>] item: string,
         [<Queue("tool-update-person")>] queue: ICollector<string>,
         log: ILogger) = 

         let fetchNetids (tool:Tool) = async {
            let! adPromise = data.GetADGroupMembers tool.ADPath |> Async.StartChild
            let! dbPromise = data.GetAllToolUsers tool |> Async.StartChild
            let! ad = adPromise
            let! db = dbPromise
            return 
                match (ad, db) with
                | (Ok(adr), Ok(dbr)) -> Ok (tool, adr, dbr) 
                | (Error(ade),_) -> Error(ade)
                | (_,Error(dbe)) -> Error(dbe)
         }

         let generateADActions (tool:Tool, ad:seq<NetId>, db:seq<NetId>) = 
            let addToAD = 
                db 
                |> Seq.except ad 
                |> Seq.map (fun a -> Add(a, tool.ADPath))
            let removeFromAD = 
                ad 
                |> Seq.except db 
                |> Seq.map (fun a -> Remove(a, tool.ADPath))
            Seq.append addToAD removeFromAD |> ok
                   
         let workflow =
            tryDeserializeAsync<Tool> HttpStatusCode.BadRequest
            >=> fetchNetids
            >=> generateADActions
            >=> tap (enqueueAll queue)

         execute workflow item

    // Pluck a tool-person from the queue. 
    // Add/remove the person to/from the specified AD group.
    [<FunctionName("ToolUpdatePersonWorker")>]
    let toolUpdatePersonWorker
        ([<QueueTrigger("tool-update-person")>] item: string,
         log: ILogger) = 
         ()