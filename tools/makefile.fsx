#I "packages/FAKE/tools"
#I "packages/Octokit/lib/net45/"
#I "packages/Microsoft.Net.Http/lib/net45/"
#I "packages/Microsoft.Bcl.Immutable/lib/portable-net45+win8+wp8/"
#r "FakeLib.dll"
#r "Octokit.dll"

open Fake
open Fake.AssemblyInfoFile

let msbuild15ProjFiles =
   !! "./PclUnit/*.csproj" //PclUnit
   ++ "./Runner/pclunit-runner/*.csproj" //AggregateRunner

let projFiles =
    msbuild15ProjFiles
    ++ "./Contrib/PclUnit.*/*.*proj" //Styles
    ++ "./Runner/Platforms/**/*-xap*.csproj" //environment first
    ++ "./Runner/Platforms/**/*.csproj" //platform runners
    -- if isMono then
          "./Runner/Platforms/sl/**/*.csproj" //sliverlight runners
       else
          "./runner/Platforms/mono*/**/*.csproj" //mono runners
    ++ "./WhoTestsTheTesters/**/*.*proj"
    -- if isMono then
          "./WhoTestsTheTesters/Tests/Silverlight*/*.csproj"
       else
          "./WhoTestsTheTesters/Tests/Mono*/*.csproj"


Target "Clean" (fun () ->
    trace " --- Cleaning stuff --- "

    ["./build"; "./deploy/build"]
      |> Seq.iter CleanDir

//    let buildMode = getBuildParamOrDefault "buildMode" "Release"
//    let setParams defaults =
//          { defaults with
//                Verbosity = Some(Quiet)
//                Targets = ["Clean"]
//                Properties =
//                    [
//                        "Optimize", "True"
//                        "DebugSymbols", "True"
//                        "Configuration", buildMode
//                        "FSharpBad", isMono.ToString()
//                    ]
//             }
//    projFiles
//      |> Seq.iter (build setParams)

)

let version ver =
  let trav = environVarOrNone "TRAVIS_BUILD_NUMBER"
  let appVey = environVarOrNone "APPVEYOR_BUILD_NUMBER"

  match trav, appVey with
      | Some(x),_ -> ver + "0", ver + x + " (Travis-CI)"
      | _,Some(x) -> ver + "0", ver + x + " (AppVeyor-CI)"
      | _,_       -> ver + "0", ver + "0 (Built Locally)"

let mainVer, mainInfoVer = version "1.0.6."

Target "Build" (fun () ->
    trace " --- Building the app --- "

    let runVer,runInfoVer = version "0.8.0."


    let createVersionInfo (dir, ext, ver, infoVer) =
      let outName = (dir + "VersionInfo." + ext)
      match ext with
        | "cs" ->
          CreateCSharpAssemblyInfo outName
            [
             Attribute.Version ver
             Attribute.FileVersion ver
             Attribute.InformationalVersion infoVer
            ]
        | "fs" ->
          CreateFSharpAssemblyInfo outName
            [
             Attribute.Version ver
             Attribute.FileVersion ver
             Attribute.InformationalVersion infoVer
            ]
        | ___ -> failwith (ext + " is not expected")

    [
      "./Contrib/PclUnit.Constraints/Properties/", "cs", mainVer, mainInfoVer
      "./Contrib/PclUnit.Style.FsUnit/", "fs" ,mainVer, mainInfoVer
      "./Contrib/PclUnit.Style.Nunit/Properties/", "cs", mainVer, mainInfoVer
      "./Contrib/PclUnit.Style.Xunit/Properties/", "cs", mainVer, mainInfoVer
      "./Runner/Platforms/shared/", "cs", runVer, runInfoVer

    ] |> Seq.iter createVersionInfo




    let buildMode = getBuildParamOrDefault "buildMode" "Release"
    let setParams defaults =
            { defaults with
                Verbosity = Some(Quiet)
                Targets = ["Build"]
                Properties =
                    [
                        "Optimize", "True"
                        "DebugSymbols", "True"
                        "Configuration", buildMode
                        "FSharpBad", isMono.ToString()
                    ]
             }

    projFiles
      |> Seq.iter (build setParams)

    !! "./Runner/pclunit-runner/bin/Release/net451/*" |> CopyFiles "./build/tools/"

    "./deploy/platforms.yml" |> CopyFile "./build/tools/platforms.yml"
)

Target "Test" (fun () ->
    trace " --- Testing app --- "

    let buildMode = getBuildParamOrDefault "buildMode" "Release"

    let args =
      sprintf
        "runConfig ./WhoTestsTheTesters/Tests/RunTests%s.yml -o ./deploy/build/test-output.json --noerror -d Configuration=%s"
        (if isMono then ".mono" else "") buildMode

    directExec (fun info ->
                       info.FileName <- "./build/tools/pclunit-runner.exe"
                       info.Arguments <- args) |> ignore

    if not <| directExec (fun info ->
                       info.FileName <- "./WhoTestsTheTesters/ConventionTestProcessor/bin/Release/ConventionTestProcessor.exe"
                       info.Arguments <- "./deploy/build/test-output.json") then
       failwith "Tests Failed"

)

Target "RestorePackages" (fun () ->
    trace " --- Restore Packages --- "
    !! "./**/packages.config"
    ++ "./**/packages.*.config"
    -- "./tools/*"
      |> Seq.iter (RestorePackage id)

    
    let restoreProj = fun args ->
                   directExec (fun info ->
                       info.FileName <- "msbuild"
                       info.Arguments <- "/t:restore " + args) |> ignore
    
    msbuild15ProjFiles 
      |> Seq.iter restoreProj
    
)

Target "Deploy:Build" (fun () ->
    trace " --- Deploying app --- "

    let outputName = sprintf "./deploy/build/Binaries.%s.zip" (if isMono then "mono" else "win")
    !! "./build/**/*"
      |> Zip "./build/" outputName

    if (Fake.Git.Information.getBranchName ".") = "stable" then
      trace " On Stable deploying to git hub"
    else
      trace " Not on Stable "

)

Target "Deploy:Nuget" (fun () ->
    trace " --- Deploying app --- "

)

"Clean"
  ==> "RestorePackages"
  ==> "Build"
  ==> "Test"
  ==> "Deploy:Build"


RunTargetOrDefault "Deploy:Build"
