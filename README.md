# License Inspector

License Inspector finds all dependencies for .NET solutions or JavaScript projects, detects their
licenses and generates a report with violations based on rules given by the
user.

## About

At many companies there might be rules regarding which external dependencies
may be taken based on their licensing terms. Keeping track of the licenses of all
dependencies and sub-dependencies across all used versions is not a fun manual
task.

This project tries to solve this problem in a resonably slim and pragmatic way.

There are many good commercial options available where this functionality is 
provided, often in combination with security analysis. See [Software Composition Analysis](https://www.google.com/search?q=Software+Composition+Analysis+license&oq=Software+Composition+Analysis)
if you need something with commercial support.

## Example output 

JSON:
```javascript
[
  {
    "Package": {
      "Id": "Newtonsoft.Json",
      "Version": "12.0.3",
      "Messages": [],
      "State": "Ok",
      "License": "MIT",
      "Result": "Ok",
      "Remark": ""
    },
    "Dependencies": []
  },
  {
    "Package": {
      "Id": "Serilog.Sinks.Console",
      "Version": "3.1.1",
      "Messages": [],
      "State": "Ok",
      "License": "Apache-2.0",
      "Result": "Ok",
      "Remark": ""
    },
    "Dependencies": [
      {
        "Package": {
          "Id": "Serilog",
          "Version": "2.5.0",
          "Messages": [],
          "State": "Ok",
          "License": "Apache-2.0",
          "Result": "Ok",
          "Remark": ""
        },
        "Dependencies": []
      }
    ]
  }
  ...
]
```

Human readable:
```
- CommandLineParser 2.7.82                                         MIT                  Ok
- Newtonsoft.Json 12.0.3                                           MIT                  Ok
- NuGet.Protocol 5.4.0                                      Apache-2.0                  Ok
 - NuGet.Packaging 5.4.0                                    Apache-2.0                  Ok
   - NuGet.Configuration 5.4.0                               Apache-2.0                  Ok
     - NuGet.Common 5.4.0                                     Apache-2.0                  Ok
       - NuGet.Frameworks 5.4.0                                Apache-2.0                  Ok
   - NuGet.Versioning 5.4.0                                  Apache-2.0                  Ok
   - Newtonsoft.Json 9.0.1                                          MIT                  Ok
- Roslynator.Analyzers 2.2.0                                Apache-2.0                  Ok
- Roslynator.CSharp 1.0.0                                   Apache-2.0                  Ok
 - Roslynator.Core 1.0.0                                    Apache-2.0                  Ok
- SemanticVersion 2.1.0                                            MIT                  Ok
- Serilog 2.9.0                                             Apache-2.0                  Ok
```

## Getting started

The application needs at least .NET Core 3.1 SDK, see [.NET SDKs for Visual Studio](https://dotnet.microsoft.com/download/visual-studio-sdks)


The shipped config and license policies will likely not suite you, but when you
have gotten everything up-and-running, it can be easily tweaked to suite your
needs.


> git clone https://github.com/edespong/license-inspector.git

> cd license-inspector

> dotnet run --project src/Main/LicenseInspector.Main.csproj -- -r . -c src/Main/config.json -t dotnet -p

This will run the application on the source code for the application.

Note that if you are targeting .NET, the first run will download a lot of items from nuget.org which will
take a significant amount of time (around 9500 files). If you want to reduce the time for this, you
can run the following script.

### Windows
> New-Item -ItemType Directory -Force c:\temp\licence-inspector | Out-Null

> Invoke-WebRequest https://www.dropbox.com/s/017ho23wr6zt66f/nugetPagesCache.zip?dl=1 -OutFile c:\temp\licence-inspector\nugetPagesCache.zip

> Expand-Archive c:\temp\licence-inspector\nugetPagesCache.zip c:\temp\licence-inspector

> Remove-Item c:\temp\licence-inspector\nugetPagesCache.zip

## License detection

For NuGet packages, licenses are detected in roughly the following way:
* If the package has a license set manually by [config](src/Main/publicPackagePolicies.json), use it
* If the package metadata contains a valid SPDX license, use it
* If the package metadata points to a known license URL, which can be set by [config](src/Main/licenses.json), use that license
* Download the file pointed to by the package license URL and run license-detector on it. If the confidence is high enough, use the license from the output

# Cache

The basics principle is that a package will never change once it has been
released. As such, the information around a package and its dependencies is
cached when fetched the first time.

If you are running the application in such a way that the environment is
continously set up and torn down, you should set a stable cache path such
as a network share or similar.

