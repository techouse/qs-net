## 1.0.2

* [FIX] change `Qs.Decode` return from `Dictionary<object, object?>` to `Dictionary<string, object?>` 
    - This change ensures that the query string decoding returns a dictionary with string keys, improving type safety and consistency in handling query parameters.

## 1.0.1

* [CHORE] Fixed CI pipeline issues in GitHub workflows
    - Added version extraction from csproj files in publish workflow
    - Fixed NuGet package push command to use specific package file instead of wildcard
    - Simplified release workflow by removing unused NAME variable
    - Added retention-days (7) to all artifact uploads to prevent storage buildup
    - Cleaned up workflow formatting and removed redundant steps

## 1.0.0

* [CHORE] Initial release of the project.
