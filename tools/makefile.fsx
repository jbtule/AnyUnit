#I "packages/FAKE/tools"
#r "FakeLib.dll"

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

    let buildMode = getBuildParamOrDefault "buildMode" "Release"
    let setParams defaults =
            { defaults with
                Verbosity = Some(Quiet)
                Targets = ["Clean"]
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

)

Target "Test" (fun () ->
    trace " --- Testing app --- "

    "./Runner/pclunit-runner/platforms.release.yml" |> CopyFile "./Runner/pclunit-runner/bin/Release/platforms.yml"

    let args =
      sprintf
        "runConfig ./WhoTestsTheTesters/Tests/RunTests%s.yml -o test.json --noerror"
        (if isMono then ".mono" else "")

    directExec (fun info ->
                       info.FileName <- "./Runner/pclunit-runner/bin/Release/pclunit-runner.exe"
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

Target "Deploy" (fun () ->
    trace " --- Deploying app --- "
)

"Clean"
  ==> "RestorePackages"
  ==> "Build"
  ==> "Test"
  ==> "Deploy"


RunTargetOrDefault "Deploy"
