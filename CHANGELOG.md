## 1.0.6

* [FIX] remove unused regex for dot-to-bracket key parsing in Decoder
* [FIX] use Sentinel encoding methods for charset detection in query serialization
* [CHORE] refactor boolean serialization to use switch expression in Encoder
* [CHORE] update documentation of `EncodeOptions` and `DecodeOptions`
* [CHORE] refactor test lambdas to discard unused parameters and simplify logic
* [CHORE] refactor test data sources to use strongly-typed TheoryData and EndToEndTestCase

## 1.0.5

* [FEAT] add key-aware decoding to the query string parser

## 1.0.4

* [FIX] optimize Encoder by caching sequence materialization and reducing allocations
* [FIX] improve Encoder performance by reducing allocations and optimizing key extraction for collections and dictionaries
* [FIX] optimize dictionary traversal by replacing nested ifs with guard clauses in Utils
* [FIX] optimize Decoder by reducing allocations and improving parameter limit handling
* [FIX] optimize Utils by reducing allocations and improving collection handling
* [FIX] optimize Qs by reducing allocations and improving collection handling
* [FIX] optimize Decoder by reducing allocations and improving string handling
* [FIX] optimize Encoder by reducing allocations and improving collection handling
* [FIX] optimize SideChannelFrame by reducing allocations and improving map handling
* [FIX] optimize HexTable by generating percent-encoded strings programmatically to reduce allocations
* [FIX] optimize HexTable by using string.Create on supported frameworks to reduce allocations
* [FIX] optimize Decoder by delaying StringBuilder allocation in JoinAsCommaSeparatedStrings to reduce memory usage
* [FIX] optimize ConvertNestedDictionary by reducing type checks and improving traversal for dictionaries and lists
* [CHORE] add documentation to ConvertNestedDictionary and ReferenceEqualityComparer for improved code clarity
* [CHORE] reformat conditional and loop blocks for improved readability in Utils
* [CHORE] reformat switch statement for improved readability in Utils
* [CHORE] add unit test for ToStringKeyDeepNonRecursive to verify conversion of nested lists and dictionaries

## 1.0.3

* [CHORE] make package .NET Standard 2.0 compatible

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
