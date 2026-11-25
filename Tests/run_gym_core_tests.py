"""Compile and run the independent anxiety, progress and approach-state models."""
from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile

project = Path(__file__).resolve().parents[1]
unity = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    "/Applications/Unity/Hub/Editor/6000.0.43f1/Unity.app/Contents"
)
dotnet = unity / "NetCoreRuntime" / ("dotnet.exe" if os.name == "nt" else "dotnet")
runtime = sorted((unity / "NetCoreRuntime/shared/Microsoft.NETCore.App").iterdir())[-1]
output = Path(tempfile.mkdtemp(prefix="gym-core-tests-"))
assembly = output / "GymCoreModelTests.dll"
arguments = [str(dotnet), str(unity / "DotNetSdkRoslyn/csc.dll"), "-nologo",
             "-target:exe", "-out:" + str(assembly)]
arguments += ["-r:" + str(p) for p in sorted(runtime.glob("*.dll"))]
arguments += [str(project / "Assets/script/GymCore/GymCoreModels.cs"),
              str(project / "Tests/GymCoreModelTests.cs")]
environment = os.environ.copy()
environment["DOTNET_CLI_HOME"] = str(output / "dotnet-home")
environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
subprocess.run(arguments, check=True, env=environment)
(output / "GymCoreModelTests.runtimeconfig.json").write_text(json.dumps({
    "runtimeOptions": {"tfm": "net6.0", "framework": {
        "name": "Microsoft.NETCore.App", "version": runtime.name}}
}))
subprocess.run([str(dotnet), str(assembly)], check=True, env=environment)
