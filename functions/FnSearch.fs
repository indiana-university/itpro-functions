namespace MyFunctions.Search

open Chessie.ErrorHandling
open MyFunctions.Types
open MyFunctions.Common
open MyFunctions.Database
open Microsoft.AspNetCore.Http
open Microsoft.Azure.WebJobs.Host
open System.Net
open System.Net.Http

module GetSimple =

    let workflow (req: HttpRequest) config (getSearchResults: string -> AsyncResult<SimpleSearch,Error>) = asyncTrial {
        let! _ = requireMembership config req
        let! term = getQueryParam "term" req
        let! result = getSearchResults term
        return result |> jsonResponse Status.OK
    }

    let run (req: HttpRequest) (log: TraceWriter) (data: IDataRepository) config = async {
        let! result = workflow req config (data.GetSimpleSearchByTerm) |> Async.ofAsyncResult
        return constructResponse log result
    }
