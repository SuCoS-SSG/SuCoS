# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

[Unreleased]

- `term.liquid` and `taxonomy.liquid` are now available for theming
- Added: YAML front matter and site settings are now case-insensitive

v[4.0.2] 2024-05-09

- Fixed crashes on serve
- Changed several code style parameters

v[4.0.1] 2024-04-25

- Added CHANGELOG.md

v[4.0.0] 2024-04-11

- Added `SuCoS new-theme` command to scaffold a new theme
- Added CODE_OF_CONDUCT.md
- Added CONTRIBUTING.md
- Added ReadyToRun option for the publish compilation
- Changed `themes` folder to allow multiple themes #BreakingChange
- Changed `SuCoS newsite` command changed to `SuCoS new-site`
- Changed README.md
- Fixed 404 page Exception
- Fixed CLI returning 1 for help and version

v[3.0.0] 2024-04-04

- Added `SuCoS newsite` command to scaffold a new site
- Added `SuCoS checklinks` command
- Added Alpine linux container image
- Changed default container images from `ubuntu` to `mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled`
- Changed the ASCII logo in the README and CLI

v[2.4.0] 2024-03-15

- Added `SuCoS build` command as the default CLI command
- Added `SuCoS build`'s `--source` as an argument
- Fixed CLI exceptions when something goes wrong

v[2.3.0] 2023-11-16

- Changed the .NET to version 8

v[2.2.1] 2023-10-19

- Added Page resources
- Added Page resources definition
- Changed replaced Microsoft.AspNetCore for System.Net

v[2.2.0] 2023-08-17 - Mage Merlin

- Added Bundle pages #BreakingChange

v[2.1.0] 2023-07-20

- Added `Draft` page variable
- Added `site` variable to be used in templates

v[2.0.0] 2023-07-13

- Added `Plain` page template variable
- Added `WordCount` page template variable
- Added `page.Site.Description` site template variable
- Added `page.Site.Copyright` site template variable
- Added `ExpiredDate` page variable

v[1.3.1] 2023-07-06

v[1.3.0] 2023-07-06

- Added `Weight` page variable
- Added `IsHome` page variable
- Added `IsPage` page variable
- Added `IsSection` page variable
- Added `Parent` page variable
- Added `static` folder to holder static content
- Added default `--output` value for build
- Added test coverage reports

v[1.2.0] 2023-06-29

- Added automated tests

v[1.1.0] 2023-06-22

- Added extra Markdown functionalities, like tables, citations, figures, mathematical formulas, auto-links
- Added `Params` page variable
- Added `URL` page variable that uses Liquid template to access page tokens
- Added `Aliases` page variables to allow alternative urls
- Added `Date` page variable
- Added `PublishDate` page variable
- Added `ExpiryDate` page variable
- Added `LastMod` page variable
- Added `-f/--future` CLI command for build and serve
- Added Section pages

v[1.0.0] 2023-07-15 - Born to be Wild

- Added First Commit!

[Unreleased]: https://gitlab.com/sucos/sucos/-/compare/v4.0.2...HEAD
[4.0.2]: https://gitlab.com/sucos/sucos/-/compare/v4.0.1...v4.0.2
[4.0.1]: https://gitlab.com/sucos/sucos/-/compare/v4.0.0...v4.0.1
[4.0.0]: https://gitlab.com/sucos/sucos/-/compare/v3.0.0...v4.0.0
[3.0.0]: https://gitlab.com/sucos/sucos/-/compare/v2.4.0...v3.0.0
[2.4.0]: https://gitlab.com/sucos/sucos/-/compare/v2.3.0...v2.4.0
[2.3.0]: https://gitlab.com/sucos/sucos/-/compare/v2.2.1...v2.3.0
[2.2.1]: https://gitlab.com/sucos/sucos/-/compare/v2.2.0...v2.2.1
[2.2.0]: https://gitlab.com/sucos/sucos/-/compare/v2.1.0...v2.2.0
[2.1.0]: https://gitlab.com/sucos/sucos/-/compare/v2.0.0...v2.1.0
[2.0.0]: https://gitlab.com/sucos/sucos/-/compare/v1.3.1...v2.0.0
[1.3.1]: https://gitlab.com/sucos/sucos/-/compare/v1.3.0...v1.3.1
[1.3.0]: https://gitlab.com/sucos/sucos/-/compare/v1.2.0...v1.3.0
[1.2.0]: https://gitlab.com/sucos/sucos/-/compare/v1.1.0...v1.2.0
[1.1.0]: https://gitlab.com/sucos/sucos/-/compare/v1.0.0...v1.1.0
[1.0.0]: https://gitlab.com/sucos/sucos/-/tree/v1.0.0
