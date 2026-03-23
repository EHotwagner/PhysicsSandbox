module TestHelpers

open System
open System.Reflection
open Xunit

let getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

let assertContains (members: string[]) (name: string) =
    Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")
