// Copyright (C) 2018 The Trustees of Indiana University
// SPDX-License-Identifier: BSD-3-Clause

namespace Functions

open Core.Types
open Core.Fakes


module FakesRepository =

    /// A canned data implementation of IDatabaseRespository (for testing)
    let FakePeople = {
        TryGetId = fun netId -> stub (swanson.NetId, Some(swanson.Id))
        GetAll = fun query -> stub ([ swanson ] |> List.toSeq)
        GetAllWithHr = fun query -> stub ([ swanson ] |> List.toSeq)
        GetHr = fun netid -> stub swanson
        GetById = fun id -> stub swanson
        GetByNetId = fun id -> stub swanson
        Create = fun person -> stub swanson
        GetMemberships = fun id -> stub ([ swansonMembership ] |> List.toSeq)
        Update = fun person -> stub swanson
    }

    let FakeUnits = {
        GetAll = fun query -> stub ([ parksAndRec ] |> List.toSeq)
        Get = fun id -> stub parksAndRec
        GetMembers = fun unit -> stub ([ swansonMembership ] |> List.toSeq) 
        GetChildren = fun unit -> stub ([ fourthFloor ] |> List.toSeq) 
        GetSupportedDepartments = fun unit -> stub ([ supportRelationship ] |> List.toSeq) 
        GetSupportedBuildings = fun unit -> stub ([ buildingRelationship ] |> List.toSeq) 
        GetDescendantOfParent = fun (parentId, childId) -> stub None
        Create = fun req -> stub parksAndRec
        Update = fun req -> stub parksAndRec
        Delete = fun req -> stub ()
    }

    let FakeDepartments = {
        GetAll = fun query -> stub ([ parksDept ] |> List.toSeq)
        Get = fun id -> stub parksDept
        GetMemberUnits = fun id -> stub ([ parksAndRec ] |> List.toSeq)
        GetSupportingUnits = fun id -> stub ([ supportRelationship ] |> List.toSeq)
    }

    let FakeMembershipRepository : MembershipRepository = {
        Get = fun id -> stub knopeMembership
        GetAll = fun () -> stub ([ knopeMembership ] |> List.toSeq) 
        Create = fun req -> stub knopeMembership
        Update = fun req -> stub knopeMembership
        Delete = fun id -> stub ()
    }

    let FakeMemberToolsRepository : MemberToolsRepository = {
        Get = fun id -> stub memberTool
        GetAll = fun () -> stub ([ memberTool ] |> List.toSeq) 
        Create = fun req -> stub memberTool
        Update = fun req -> stub memberTool
        Delete = fun id -> stub ()
    }

    let FakeToolsRepository : ToolsRepository = {
        GetAll = fun () -> stub ([ tool ] |> List.toSeq)
        GetAllPermissions = fun () -> stub ([ toolPermission ] |> List.toSeq)
        Get = fun id -> stub tool
    }

    let FakeSupportRelationships : SupportRelationshipRepository = {
        GetAll = fun () -> stub ([ supportRelationship ] |> List.toSeq) 
        Get = fun id -> stub supportRelationship
        Create = fun req -> stub supportRelationship
        Update = fun req -> stub supportRelationship
        Delete = fun id -> stub ()
    }

    let FakeAuthorization : AuthorizationRepository = {
        UaaPublicKey = fun _ -> stub adminJwt.access_token
        IsServiceAdmin = fun id -> stub true
        IsUnitManager = fun netid id -> stub true
        IsUnitToolManager = fun netid id -> stub true
        CanModifyPerson = fun netid id -> stub true
    }
    
    let FakeBuildings = {
        GetAll = fun query -> stub ([ cityHall ] |> List.toSeq)
        Get = fun id -> stub cityHall
        GetSupportingUnits = fun id -> stub ([ buildingRelationship ] |> List.toSeq)        
    }

    let FakeBuildingRelationships : BuildingRelationshipRepository = {
        GetAll = fun () -> stub ([ buildingRelationship ] |> List.toSeq) 
        Get = fun id -> stub buildingRelationship
        Create = fun req -> stub buildingRelationship
        Update = fun req -> stub buildingRelationship
        Delete = fun id -> stub ()
    }

    let FakeLegacy = {
        GetLspList = fun () -> stub { LspInfos = [|lspInfo|] }
        GetLspDepartments = fun dept -> stub lspDepartments
        GetDepartmentLsps = fun dept -> stub { LspContacts = [|lspContact|] }
    }

    let Repository = {
        People = FakePeople
        Units = FakeUnits
        Departments = FakeDepartments
        Buildings = FakeBuildings
        Memberships = FakeMembershipRepository
        MemberTools = FakeMemberToolsRepository
        Tools = FakeToolsRepository
        SupportRelationships = FakeSupportRelationships
        BuildingRelationships = FakeBuildingRelationships
        Authorization = FakeAuthorization
        Legacy = FakeLegacy
    }

