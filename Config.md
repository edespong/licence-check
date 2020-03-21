# Configuration

## Package policies

The package policy argument can be a comma-separated list of one or more files.

A package policy is specified on the following format:

```javascript
{
  // Required. Can end with a *
  "Package": "<Package Identifier>",

  // Optional (default: null). Use to short-circuit the license
  // detection.
  "License": "<SPDX License Identifier>", 

  // Optional (default: false). Use to short-circuit the license
  // evaluation (no detection is done).
  "AllowLicense": <true|false>,

  // Optional (default: false). Set if the package should be ignored
  // completely. Use for example for system packages.
  "Ignore": <true|false>,

  // Optional (default: null). If there are internal packages, setting the
  // location will recursively also crawl that location for dependencies.
  "Location": "<Path to source>"
}
```

Different policies for different versions of the same package are not supported. 

See [publicPackagePolicies.json](src/Main/publicPackagePolicies.json) for an
example.

## License policies

A license policy is specified on the following format:

```javascript
{
    "License": "<SPDX License Identifier>|<unknown>|<not evaluated>|<internal>",

    // Optional (default: false). Is the license generally ok?
    "Allow": <true|false>,

    // Optional (default: false). Is the license ok for internal usage? See [Projects](#projects).
    "AllowInternal": <true|false>
}
```

`<unknown>` is a special token given to packages where the license cannot be determined.

`<not evaluated>` is a special token given to packages where we have not even tried to determine the license.

`<internal>` is a special token given to packages where Location is given in the package policy.

See [licensePolicies.json](src/Main/licensePolicies.json)

## <a name="projects"></a>Projects

The projects file allows you to specify paths to projects that are only used 
internally and not distributed. In conjunction with a License policy, this
allows you to specify that some licenses can be used if only used internally.

A typical example of this is a testing framework with a restrictive license
that you do not want to get a Violation from since it will never be distributed.

```javascript
{
  "InternalProjects": [
    "c:\\Code\\MyProject\\Tests\\UnitTest.csproj"
  ]
}
```

## LicenseInfo

Fetching license files over the internet and running the license detector can
be both expensive and error prone. The LicenseInfo config points to a file
where well known licenses can be specified. For example, a package with
a reference to http://aws.amazon.com/apache2.0 can then be immediately resolved
to Apache 2.0.

See [licenses.json](src/Main/licenses.json)

## Other configuration

See [config.json](src/Main/config.json) for the default config. If no paths have
been set for the package or license policies, the default paths are used.

### Minimum license confidence threshold

When trying to determine the license from a text, license-detector will give a
confidence level on the license returned. If the confidence level is above this
threshold the license will be accepted.

### Ignore duplicate packages

Many of your packages will have sub-dependencies that are the same. This setting
determines if a package should only be evaluated once (and show up once in the
output). If you want a full tree of all dependencies, this should be set to false.

### Follow locations

You can give a location where to find an internal package in the package policy
file. This automatically marks it as internal. If you do not want to recurively
look for other dependencies in that location, set this config to false.

### Disk cache configuration

The application caches a lot of the intermediary results to speed up subsequent
runs. The CacheRoot path should be changed to where you want the cached data to
be placed.
