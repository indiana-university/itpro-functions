namespace Functions

open Functions.Common.Types
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging
open System
open System.Net.Http

///<summary>
/// This module defines the bindings and triggers for all functions in the project
///</summary
module Functions =
    
    /// (Anonymous) A function that simply returns, "Pong!" 
    [<FunctionName("Options")>]
    let options
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "options", Route = "{*url}")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        Api.Common.optionsResponse req log context

    [<FunctionName("PingGet")>]
    let ping
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "ping")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        let fn (config, data:IDataRepository) = Api.Ping.get req
        Api.Common.getResponse req log context fn

    /// (Anonymous) Exchanges a UAA OAuth code for an application-scoped JWT
    [<FunctionName("AuthGet")>]
    let auth
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "auth")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        let fn (config, data:IDataRepository) = Api.Auth.get req config data.GetUserByNetId
        Api.Common.getResponse req log context fn

    /// (Authenticated) Get a user profile for a given user 'id'
    [<FunctionName("UserGetId")>]
    let profileGet
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "people/{id}")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext, id: Id) =
        let fn (config, data:IDataRepository) = Api.Common.getById req config id data.GetProfile
        Api.Common.getResponse req log context fn

    /// (Authenticated) Get a user profile associated with the JWT in the request Authorization header.
    [<FunctionName("UserGetMe")>]
    let profileGetMe
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "me")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) = 
        let fn (config, data:IDataRepository) = Api.User.getMe req config data.GetProfile
        Api.Common.getResponse req log context fn

    // [<FunctionName("UserPut")>]
    // let profilePut
    //     ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "put", Route = "users/{id}")>]
    //     req: HttpRequest,
    //     logger: Iloggerger,
    //     context: ExecutionContext,
    //     id: Id) =
    //         context |> appConfig |> User.Put.run req logger id |> Async.StartAsTask

    /// (Authenticated) Get all users, departments, and units that match a 'term' query.
    [<FunctionName("SearchGet")>]
    let searchSimpleGet
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "search")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        let fn (config, data:IDataRepository) = Api.Search.getSimple req config data.GetSimpleSearchByTerm
        Api.Common.getResponse req log context fn


    /// (Authenticated) Get all units.
    [<FunctionName("UnitGetAll")>]
    let unitGetAll
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "units")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        let fn (config, data:IDataRepository) = Api.Common.getAll req config data.GetUnits
        Api.Common.getResponse req log context fn
            
    /// (Authenticated) Get a unit profile for a given unit 'id'.
    [<FunctionName("UnitGetId")>]
    let unitGetId
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "units/{id}")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext, id: Id) =
        let fn (config, data:IDataRepository) = Api.Common.getById req config id data.GetUnit
        Api.Common.getResponse req log context fn
            
    /// (Authenticated) Get all departments.
    [<FunctionName("DepartmentGetAll")>]
    let departmentGetAll
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "departments")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext) =
        let fn (config, data:IDataRepository) = Api.Common.getAll req config data.GetDepartments
        Api.Common.getResponse req log context fn

    /// (Authenticated) Get a department profile for a given department 'id'.
    [<FunctionName("DepartmentGetId")>]
    let departmentGetId
        ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get", Route = "departments/{id}")>]
        req: HttpRequestMessage, log: ILogger, context: ExecutionContext, id: Id) =
        let fn (config, data:IDataRepository) = Api.Common.getById req config id data.GetDepartment
        Api.Common.getResponse req log context fn
