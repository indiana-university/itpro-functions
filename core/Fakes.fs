module Core.Fakes

open Types

// UaaResponse 
let accessToken = { access_token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOiIxNTE1NTQ0NjQzIiwidXNlcl9pZCI6MSwidXNlcl9uYW1lIjoiam9obmRvZSIsInVzZXJfcm9sZSI6ImFkbWluIn0.akuT7-xDFxrev-T9Dv0Wdumx1HK5L2hQAOU51igIjUE" }

// Units
let cityOfPawnee:Unit = {Id=1; Name="City of Pawnee"; Description="City of Pawnee, Indiana"; Url="http://pawneeindiana.com/"; ParentId=None; Parent=None}
let parksAndRec:Unit = {Id=2; Name="Parks and Rec"; Description="Parks and Recreation"; Url="http://pawneeindiana.com/parks-and-recreation/"; ParentId=Some(cityOfPawnee.Id); Parent=Some(cityOfPawnee)}
let fourthFloor:Unit = {Id=3; Name="Fourth Floor"; Description="City Hall's Fourth Floor"; Url="http://pawneeindiana.com/fourth-floor/"; ParentId=Some(cityOfPawnee.Id); Parent=Some(cityOfPawnee)}
let parksAndRecUnitRequest:UnitRequest = { Name="Parks and Rec"; Description="Parks and Recreation"; Url="http://pawneeindiana.com/parks-and-recreation/"; ParentId=Some(cityOfPawnee.Id) }

// Departments
let parksDept:Department = {Id=1; Name="PA-PARKS"; Description="Parks and Recreation Department" }

// People
let swanson:Person = {
    Id=1
    Hash="hash"
    NetId="rswanso"
    Name="Swanson, Ron"
    Position="Parks and Rec Director "
    Location=""
    Campus=""
    CampusPhone=""
    CampusEmail="rswanso@pawnee.in.us"
    Expertise="Woodworking; Honor"
    Notes=""
    PhotoUrl="http://flavorwire.files.wordpress.com/2011/11/ron-swanson.jpg"
    Responsibilities = Responsibilities.ItLeadership
    DepartmentId=parksDept.Id
    Department=parksDept
}

let knope:Person = {
    Id=2
    Hash="hash"
    NetId="lknope"
    Name="Knope, Lesie Park"
    Position="Parks and Rec Deputy Director "
    Location=""
    Campus=""
    CampusPhone=""
    CampusEmail="lknope@pawnee.in.us"
    Expertise="Canvasing; Waffles"
    Notes=""
    PhotoUrl="https://en.wikipedia.org/wiki/Leslie_Knope#/media/File:Leslie_Knope_(played_by_Amy_Poehler).png"
    Responsibilities = Responsibilities.ItLeadership ||| Responsibilities.ItProjectMgt
    DepartmentId=parksDept.Id
    Department=parksDept
}

let wyatt:Person = {
    Id=3
    Hash="hash"
    NetId="bwyatt"
    Name="Wyatt, Ben"
    Position="Auditor"
    Location=""
    Campus=""
    CampusPhone=""
    CampusEmail="bwyatt@pawnee.in.us"
    Expertise="Board Games; Comic Books"
    Notes=""
    PhotoUrl="https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg"
    Responsibilities = Responsibilities.ItProjectMgt
    DepartmentId=parksDept.Id
    Department=parksDept
}

let tool: Tool = 
  { Id=1
    Name="Hammer"
    Description=""
    DepartmentScoped=true }

let memberTool:MemberTool = 
  { Id=1
    MembershipId=1
    ToolId=1 }

let knopeMembershipRequest:UnitMemberRequest = {
    UnitId=parksAndRec.Id
    PersonId=Some(knope.Id)
    Role=Role.Sublead
    Permissions=UnitPermissions.Viewer
    Title="Deputy Director"
    Percentage=100
}

let swansonMembership:UnitMember = {
    Id=1
    UnitId=parksAndRec.Id
    PersonId=Some(swanson.Id)
    Unit=parksAndRec
    Person=Some(swanson)
    Title="Director"
    Role=Role.Leader
    Permissions=UnitPermissions.Owner
    Percentage=100
    MemberTools=[ memberTool ]
}

let knopeMembership = {
    Id=2
    UnitId=parksAndRec.Id
    PersonId=Some(knope.Id)
    Role=Role.Sublead
    Permissions=UnitPermissions.Viewer
    Title="Deputy Director"
    Percentage=100
    Person=Some(knope)
    Unit=parksAndRec
    MemberTools=Seq.empty
}

let parksAndRecVacancy = {
    Id=3
    UnitId=parksAndRec.Id
    PersonId=None
    Role=Role.Member
    Permissions=UnitPermissions.Viewer
    Title="Assistant to the Manager"
    Percentage=100
    Person=None
    Unit=parksAndRec
    MemberTools=Seq.empty
}

let wyattMembership:UnitMember = {
    Id=4
    UnitId=cityOfPawnee.Id
    PersonId=Some(wyatt.Id)
    Unit=cityOfPawnee
    Person=Some(wyatt)
    Title="Auditor"
    Role=Role.Leader
    Permissions=UnitPermissions.Owner
    Percentage=100
    MemberTools=Seq.empty
}

let supportRelationshipRequest:SupportRelationshipRequest = {
    UnitId=cityOfPawnee.Id
    DepartmentId=parksDept.Id
}

let supportRelationship:SupportRelationship = {
    Id=1
    UnitId=cityOfPawnee.Id
    DepartmentId=parksDept.Id
    Unit=cityOfPawnee
    Department=parksDept
}

let toolPermission:ToolPermission = {
    NetId=wyatt.NetId
    ToolName=tool.Name
    DepartmentName=parksDept.Name
}