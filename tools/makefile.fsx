#I "packages/FAKE/tools"
#I "packages/Octokit/lib/net45/"
#I "packages/Microsoft.Net.Http/lib/net45/"
#I "packages/Microsoft.Bcl.Immutable/lib/portable-net45+win8+wp8/"
#r "FakeLib.dll"
#r "Octokit.dll"

open Fake

let projFiles =
  !! "./PclUnit/*.csproj" //PclUnit
    ++ "./Contrib/PclUnit.*/*.*proj" //Styles
    ++ "./Runner/pclunit-runner/*.csproj" //AggregateRunner
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

Target "Build" (fun () ->
    trace " --- Building the app --- "
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

    "./deploy/platforms.yml" |> CopyFile "./build/tools/platforms.yml"
)

Target "Test" (fun () ->
    trace " --- Testing app --- "

    let args =
      sprintf
        "runConfig ./WhoTestsTheTesters/Tests/RunTests%s.yml -o test.json --noerror"
        (if isMono then ".mono" else "")

    directExec (fun info ->
                       info.FileName <- "./build/tools/pclunit-runner.exe"
                       info.Arguments <- args) |> ignore

    if not <| directExec (fun info ->
                       info.FileName <- "./WhoTestsTheTesters/ConventionTestProcessor/bin/Release/ConventionTestProcessor.exe"
                       info.Arguments <- "test.json") then
       failwith "Tests Failed"

)

Target "RestorePackages" (fun () ->
    trace " --- Restore Packages --- "
    !! "./**/packages.config"
    -- "./tools/*"
      |> Seq.iter (RestorePackage id)
)

Target "Deploy:Build" (fun () ->
    trace " --- Deploying app --- "

    let outputName = sprintf "./deploy/build/Binaries.%s.zip" (if isMono then "mono" else "win")
    !! "./build/**/*"
      |> Zip "./build/" outputName

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
