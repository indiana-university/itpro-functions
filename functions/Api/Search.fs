namespace MyFunctions.Api

open Chessie.ErrorHandling
open MyFunctions.Common.Types
open MyFunctions.Common.Jwt
open MyFunctions.Common.Http
open System.Net.Http

module Search =
    /// <summary>
    /// Conduct a simple search of units, departments, and users. 
    /// Will return any unit, department, or user whose name matches any part of 
    /// the 'term' query parameter.
    /// </summary>
    /// <param name="req">The HTTP request that triggered this function</param>
    /// <param name="config">The application configuration</param>
    /// <param name="getSearchResults">A function to fetch matching units, departments, and users provided some search term.</param>
    /// <returns>
    /// A JSON-encoded search result.
    /// </returns>
    let getSimple (req: HttpRequestMessage) config (getSearchResults: string -> AsyncResult<SimpleSearch,Error>) = asyncTrial {
        let! _ = requireMembership config req
        let! term = getQueryParam "term" req
        let! result = getSearchResults term
        return result |> jsonResponse Status.OK
    }
