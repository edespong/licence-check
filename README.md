# License Inspector

License Inspector finds all dependencies for .NET solutions, detects their
licenses and generates a report with violations based on rules given by the
user.

## About

At many companies there might be rules regarding which external dependencies
may be taken based on their licensing terms. Keeping track of the licenses of all
dependencies and sub-dependencies across all used versions is not a fun manual
task.

This project tries to solve this problem in a resonably slim and pragmatic way.

There are many good commercial options available where this functionality is 
provided, often in combination with security analysis. See https://www.google.com/search?q=Software+Composition+Analysis+license&oq=Software+Composition+Analysis
if you need something with commercial support.


## Getting started

The applcation needs at least .NET Core 3.1 SDK, see [.NET SDKs for Visual Studio](https://dotnet.microsoft.com/download/visual-studio-sdks)


The shipped config and license policies will likely not suite you, but when you
have gotten everything up-and-running, it can be easily tweaked to suite your
needs.


> git clone https://github.com/edespong/licence-inspector.git

> cd license-inspector

> dotnet run --project src/Main/LicenseInspector.Main.csproj -- -r . -c src/Main/config.json -p

This will run the application on the source code for the application.

Note that the first run will download a lot of items from nuget.org which will
take a significant amount of time. If you want to reduce the time for this, you
can run the following script.

### Windows
> New-Item -ItemType Directory -Force c:\temp\licenceInspector | Out-Null

> Invoke-WebRequest https://www.dropbox.com/s/017ho23wr6zt66f/nugetPagesCache.zip?dl=1 -OutFile c:\temp\licenceInspector\nugetPagesCache.zip

> Expand-Archive c:\temp\licenceInspector\nugetPagesCache.zip c:\temp\licence-inspector

> Remove-Item c:\temp\licenceInspector\nugetPagesCache.zip

## License detection

For NuGet packages, licenses are detected in roughly the following way:
* If the package has a license set manually by ##config##, use it
* If the package metadata contains a valid SPDX license, use it
* If the package metadata points to a known license URL, which can be set by ##config##, use that license
* Download the file pointed to by the package license URL and run license-detector on it. If the confidence is high enough, use the license from the output

# Cache

The basics principle is that a package will never change once it has been
released. As such, the information around a package and its dependencies is
cached when fetched the first time.

If you are running the application in such a way that the environment is
continously set up and torn down, you should set a stable cache path such
as a network share or similar.

