namespace Functions.Common

open Chessie.ErrorHandling
open Types
open Util
open Dapper
open Npgsql

module Database =
    let private like (term:string)  = 
        term.Replace("[", "[[]").Replace("%", "[%]") 
        |> sprintf "%%%s%%"

    type IdFilter = {
        Id: Id
    }

    type NetIdFilter = {
        NetId: NetId
    }

    /// Fetch a user given a netid (e.g. 'jhoerr')
    let queryUserByNetId connStr netId = asyncTrial {
        let fn () = async {
            use cn = new NpgsqlConnection(connStr)
            let! seq = cn.GetListAsync<User>({NetId=netId}) |> Async.AwaitTask
            return ok seq
        }
        let! result = tryfAsync Status.InternalServerError "Failed to fetch user by netId" fn
        let! head = tryGetFirst result (sprintf "No user found with netid '%s'" netId)
        return head
    }

    /// Fetch a single 'T given an ID
    let queryTypeById<'T> connStr id = asyncTrial {
        let fn () = async {
            use cn = new NpgsqlConnection(connStr)
            let! result = cn.GetAsync<'T>(id) |> Async.AwaitTask
            match box result with
            | null -> return fail (Status.NotFound, sprintf "No %s found with id %d" (typeof<'T>.Name) id)
            | _ -> return ok result
        }
        let! result = tryfAsync Status.InternalServerError (sprintf "Failed to fetch %s by id %d" (typeof<'T>.Name) id) fn
        return result
    }

    /// Fetch all 'T
    let queryAll<'T> connStr query msg id = asyncTrial {
        let fn() = async {
            use cn = new NpgsqlConnection(connStr)
            let! seq = cn.QueryAsync<'T>(query, {Id=id}) |> Async.AwaitTask
            return seq |> Seq.cast<'T> |> ok
        }
        let! result = tryfAsync Status.InternalServerError msg fn
        return result        
    }

    /// Get all departments supported by a given unit ID
    let queryDepartmentsSupportedByUnit connStr unitId = asyncTrial {
        let query = """
SELECT d.Id, d.Name, d.Description FROM Departments d
JOIN SupportedDepartments sd on d.Id = sd.DepartmentId 
WHERE sd.UnitId = @Id
GROUP BY d.Id, d.Name, d.Description
ORDER BY d.Name ASC"""
        let msg =  "Failed to queryDepartmentsSupportedByUnit"
        let! result = queryAll<Department> connStr query msg unitId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get all organizational units that exist within a given department ID
    let queryOrgUnitsInDepartment connStr deptId = asyncTrial {
        let query = """
SELECT un.Id, un.Name, un.Description FROM Units un
JOIN UnitMembers m on un.Id = m.UnitId
JOIN Users u on u.Id = m.UserId
WHERE u.HrDepartmentId = @Id 
GROUP BY un.Id, un.Name, un.Description
ORDER BY un.Name ASC"""
        let msg = "Failed to queryOrgUnitsInDepartment"
        let! result = queryAll<Unit> connStr query msg deptId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get all supporting units for a given department ID

    let queryUnitsSupportingDepartment connStr deptId = asyncTrial {
        let query = """
SELECT un.Id, un.Name, un.Description FROM Units un
JOIN SupportedDepartments sd on un.Id = sd.UnitId
WHERE sd.DepartmentId = @Id 
GROUP BY un.Id, un.Name, un.Description
ORDER BY un.Name ASC"""
        let msg = "Failed to queryUnitsSupportingDepartment"
        let! result = queryAll<Unit> connStr query msg deptId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get all people with an HR relationship to a given department ID

    let queryPeopleInDepartment connStr deptId = asyncTrial {
        let query = """
SELECT u.Id, u.Name FROM Users u
WHERE u.HrDepartmentId = @Id
ORDER BY u.Name ASC"""
        let msg = "Failed to queryPeopleInDepartment"
        let! result = queryAll<Member> connStr query msg deptId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get all people with a relationship to a given unit ID
    let queryPeopleInUnit connStr unitId = asyncTrial {
        let query = """
SELECT u.Id, u.Name, u.Role FROM Users u
JOIN UnitMembers m ON u.Id = m.UserId 
WHERE m.UnitId = @Id
ORDER BY u.Name ASC"""
        let msg = "Failed to queryPeopleInUnit"
        let! result = queryAll<MemberWithRole> connStr query msg unitId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get all units with a relationship to a given person ID
    let queryUnitMemberships connStr userId = asyncTrial {
        let query = """
SELECT u.Id, u.Name, u.Url FROM Units u
JOIN UnitMembers m ON u.Id = m.UnitId
WHERE m.UserId = @Id
ORDER BY u.Name ASC"""
        let msg = "Failed to queryUnitMemberships"
        let! result = queryAll<MemberWithRole> connStr query msg userId
        return result |> Seq.sortBy (fun u -> u.Name)
    }

    /// Get the profile for a given user ID
    let queryUserProfile connStr id = asyncTrial {
        let! user = queryTypeById<User> connStr id
        let! unitMemberships = queryUnitMemberships connStr id
        let! dept = queryTypeById<Department> connStr user.HrDepartmentId
        let profile = {
            User=user
            Department=dept
            UnitMemberships=unitMemberships
        }       
        return profile
    }

    type SimpleSearchQuery = {
        Term: string
    }

    /// Get all 'T whose name matches a given search term
    let querySearch<'T> connStr term conditions = asyncTrial {
        let fn() = async {
            use cn = new NpgsqlConnection(connStr)
            let! seq = cn.GetListAsync<'T>(conditions, {Term=(like term)})  |> Async.AwaitTask
            return ok seq
        }
        let! result = tryfAsync Status.InternalServerError (sprintf "Failed to get %s by search term" typeof<'T>.Name ) fn
        return result
    }

    /// Get all people, departments, and units whose name matches a given search term
    let querySimpleSearch connStr term = asyncTrial {
        let! users = querySearch<User> connStr term "WHERE Name LIKE @Term OR NetId LIKE @Term"
        let! units = querySearch<Unit> connStr term "WHERE Name LIKE @Term OR Description LIKE @Term"
        let! depts = querySearch<Department> connStr term "WHERE Name LIKE @Term OR Description LIKE @Term"
        return {
            Users=users |> Seq.sortBy (fun u -> u.Name)
            Units=units |> Seq.sortBy (fun u -> u.Name)
            Departments=depts |> Seq.sortBy (fun u -> u.Name)
        }
    }

    /// Get a list of all units
    let queryUnits connStr = asyncTrial {
        let fn () = async {
            use cn = new NpgsqlConnection(connStr)
            let! seq = cn.GetListAsync<Unit>() |> Async.AwaitTask
            return seq |> Seq.sortBy (fun u -> u.Name) |> ok 
        }
        let! result = tryfAsync Status.InternalServerError "Failed to fetch user by netId" fn
        return result
    }

    type UnitQuery = {
        UnitId: Id
    }

    /// Get a single unit by ID
    let queryUnit connStr id = asyncTrial {
        let! unit = queryTypeById<Unit> connStr id
        // let! members = queryPeopleInUnit connStr id
        // let! supportedDepartments = queryDepartmentsSupportedByUnit connStr id
        return {
            Unit=unit
            //Members=members
            //SupportedDepartments=supportedDepartments
        }
    }

    /// Get a list of all departments
    let queryDepartments connStr = asyncTrial {
        let fn () = async {
            use cn = new NpgsqlConnection(connStr)
            let! seq = cn.GetListAsync<Department>() |> Async.AwaitTask
            return seq |> Seq.sortBy (fun u -> u.Name) |> ok 
        }
        let! result = tryfAsync Status.InternalServerError "Failed to fetch user by netId" fn
        return result
    }

    /// Get a single department by ID
    let queryDepartment connStr id = asyncTrial {
        let! dept = queryTypeById<Department> connStr id
        let! members = if (not dept.DisplayUnits) then queryPeopleInDepartment connStr id else emptySeq<Member>()
        let! orgUnits = if dept.DisplayUnits then queryOrgUnitsInDepartment connStr id else emptySeq<Unit>()
        let! supportingUnits = queryUnitsSupportingDepartment connStr id
        return {
            Department=dept
            Units = orgUnits
            Members = members
            SupportingUnits=supportingUnits
        }
    }

    /// A SQL Database implementation of IDatabaseRespository
    type DatabaseRepository(connectionString:string) =
        let connStr = connectionString

        interface IDataRepository with 
            member this.GetUserByNetId netId = queryUserByNetId connStr netId
            member this.GetProfile id = queryUserProfile connStr id
            member this.GetSimpleSearchByTerm term = querySimpleSearch connStr term
            member this.GetUnits () = queryUnits connStr
            member this.GetUnit id = queryUnit connStr id
            member this.GetDepartments () = queryDepartments connStr 
            member this.GetDepartment id = queryDepartment connStr id
