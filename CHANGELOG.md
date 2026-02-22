## 1.2.1

* [FIX] optimize deep encode traversal by replacing ancestor side-channel scans with O(1) active-path cycle tracking
* [FIX] reduce deep encode allocations by switching path assembly to cached segment nodes with leaf-time materialization
* [CHORE] add encode deep-nesting benchmark harness and soft performance regression guard tests

## 1.2.0

* [FIX] harden encoder/decode merge paths to iterative traversal for deep-input stack safety
* [FIX] decode `byte[]` values with selected charset when `EncodeOptions.Encode = false` (including comma list paths)
* [FIX] keep `DateSerializer` and comma temporal normalization active after `FunctionFilter` transforms
* [FIX] harden decoder empty-segment/key handling before parameter-limit counting and enforce comma list-limit behavior deterministically
* [FIX] enforce runtime `EncodeOptions`/`DecodeOptions` validation for object-initializer scenarios
* [FIX] allow `Qs.Decode(...)` for `IEnumerable<KeyValuePair<string?, object?>>` input with duplicate handling aligned to `DecodeOptions`
* [FIX] avoid false cyclic-object detection when encoding shared sibling references (ancestor cycles still rejected)
* [DOCS] document intentional edge-case divergences from JavaScript `qs` where limitation fixes are preferred

## 1.1.2

* [FIX] implement `DecodeOptions.ListLimit` handling to prevent DoS via memory exhaustion

## 1.1.1

* [CHORE] stabilize .NET 10 CI tests

## 1.1.0

* [FEAT] add `EncodeOptions.CommaCompactNulls` to drop `null` entries when encoding comma lists

## 1.0.11

* [CHORE] optimize `Decoder.ParseQueryStringValues`

## 1.0.10

* [CHORE] expanded CI with multi-target smoke tests (.NET 5–9, 10 preview), Windows .NET Framework smoke, and a compile-only .NET Core 3.1 check

## 1.0.9

* [CHORE] add logo to NuGet package

## 1.0.8

* [CHORE] standardize exception handling and improve culture-invariant string formatting

## 1.0.7

* [FIX] fix degenerate bracket and dot parsing in `Decoder` for edge cases and parity
* [CHORE] add tests for top-level dot handling, depth remainder, and unterminated bracket cases in `Decoder`
* [CHORE] add tests for `SplitKeyIntoSegments` remainder and strict depth handling
* [CHORE] add tests for double dot and encoded bracket handling in `Decoder` with dot options

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
