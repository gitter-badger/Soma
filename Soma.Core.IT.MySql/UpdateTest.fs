﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2011 The Soma Team. 
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.txt file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
//----------------------------------------------------------------------------

namespace Soma.Core.IT

open System
open System.Configuration
open System.Data
open System.Data.Common
open System.Transactions
open NUnit.Framework
open Soma.Core

module UpdateTest = 

  type Department =
    { [<Id>]
      DepartmentId : int
      DepartmentName : string
      [<Version>]
      VersionNo : int }

  type Employee =
    { [<Id(IdKind.Identity)>]
      EmployeeId : int option
      EmployeeName : string option
      DepartmentId : int option
      [<Version>]
      VersionNo : int option }

  type CompKeyEmployee =
    { [<Id>]
      EmployeeId1 : int
      [<Id>]
      EmployeeId2 : int
      EmployeeName : string
      [<Version>]
      VersionNo : int }

  type NoId =
    { Name : string
      VersionNo : int }

  type NoVersion =
    { [<Id>]
      Id : int
      Name : string }

  [<Test>]
  let ``update : no id``() =
    use ts = new TransactionScope()
    try
      MySql.update { NoId.Name = "aaa"; VersionNo = 0 } |> ignore
      fail ()
    with 
    | :? InvalidOperationException as ex -> 
      printfn "%A" ex
      assert_true <| ex.Message.StartsWith "[SOMA4005]"
    | ex -> 
      fail ex

  [<Test>]
  let ``update : composite id``() =
    use ts = new TransactionScope()
    let employee = MySql.find<CompKeyEmployee> [2; 12]
    let employee = MySql.update { employee with EmployeeName = "hoge" }
    printfn "%A" employee
    assert_equal { EmployeeId1 = 2; EmployeeId2 = 12; EmployeeName = "hoge"; VersionNo = 1; } employee

  [<Test>]
  let ``update : no version``() =
    use ts = new TransactionScope()
    let noVersion = MySql.update { NoVersion.Id = 1; Name = "aaa" }
    printfn "%A" noVersion
    assert_equal { NoVersion.Id = 1; Name = "aaa" } noVersion

  [<Test>]
  let ``update : incremented version``() =
    use ts = new TransactionScope()
    let department = { DepartmentId = 1; DepartmentName = "hoge"; VersionNo = 0 }
    let department = MySql.update department
    printfn "%A" department
    assert_equal 1 department.VersionNo


  [<Test>]
  let ``update : unique constraint violation``() =
    use ts = new TransactionScope()
    let department = { DepartmentId = 1; DepartmentName = "Sales"; VersionNo = 0 }
    try
      MySql.update department |> ignore
      fail ()
    with 
    | UniqueConstraintException _ as ex -> printfn "%A" ex
    | ex -> fail ex

  [<Test>]
  let ``update : optimistic lock confliction``() =
    use ts = new TransactionScope()
    let department = { DepartmentId = 1; DepartmentName = "hoge"; VersionNo = -1 }
    try
      MySql.update department |> ignore
      fail ()
    with 
    | OptimisticLockException _ as ex -> printfn "%s" (string ex)
    | ex -> fail ex

  [<Test>]
  let ``updateIgnoreVersion``() =
    use ts = new TransactionScope()
    let department = { DepartmentId = 1; DepartmentName = "hoge"; VersionNo = -1 }
    let department = MySql.updateWithOpt department (UpdateOpt(IgnoreVersion = true))
    printfn "%A" department

  [<Test>]
  let ``updateIgnoreVersion : no affected row``() =
    use ts = new TransactionScope()
    let department = { DepartmentId = 0; DepartmentName = "hoge"; VersionNo = -1 }
    try
      MySql.updateWithOpt department (UpdateOpt(IgnoreVersion = true)) |> ignore
      fail ()
    with 
    | NoAffectedRowException _ as ex -> printfn "%s" (string ex)
    | ex -> fail ex