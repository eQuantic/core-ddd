# Changelog

## [7.2.0](https://github.com/eQuantic/core-ddd/compare/v7.1.1...v7.2.0) (2026-11-18)

### Features

* an aggregate saves the members it changed, never its whole row ([#11](https://github.com/eQuantic/core-ddd/issues/11)) ([0cd1570](https://github.com/eQuantic/core-ddd/commit/0cd1570653c143603cdbaea6829a79e235427ff3)), closes [#10](https://github.com/eQuantic/core-ddd/issues/10)

## [7.1.1](https://github.com/eQuantic/core-ddd/compare/v7.1.0...v7.1.1) (2026-11-15)

### Bug Fixes

* **deps:** the Entity Framework family without known vulnerabilities ([#9](https://github.com/eQuantic/core-ddd/issues/9)) ([ad1ee75](https://github.com/eQuantic/core-ddd/commit/ad1ee758a623935729f9c64a857ff2edf3ef8d37)), closes [#8](https://github.com/eQuantic/core-ddd/issues/8)

## [7.1.0](https://github.com/eQuantic/core-ddd/compare/v7.0.0...v7.1.0) (2026-11-15)

### Features

* move to eQuantic.Core.Data 7.1 and the Entity Framework family on it ([#7](https://github.com/eQuantic/core-ddd/issues/7)) ([78572f0](https://github.com/eQuantic/core-ddd/commit/78572f0f4f5e4ed10a81141598cd8ac34d5a94e1)), closes [#6](https://github.com/eQuantic/core-ddd/issues/6)

## [7.0.0](https://github.com/eQuantic/core-ddd/compare/v6.0.0...v7.0.0) (2026-11-14)

### ⚠ BREAKING CHANGES

* a project that calls ApplyDataModelConventions, HasJsonData or DataModelConventionOptions with only eQuantic.Core.Persistence referenced now needs eQuantic.Core.Persistence.EntityFramework (a provider package brings it). A project that used Lamar through eQuantic.Core.Application, or Humanizer through eQuantic.Core.Persistence, references it itself.

### Features

* the aggregate seam without Entity Framework, and Application without Lamar ([#5](https://github.com/eQuantic/core-ddd/issues/5)) ([1c2b88a](https://github.com/eQuantic/core-ddd/commit/1c2b88a84cfb5025d2443479c7c4cd575ace89da))

## [6.0.0](https://github.com/eQuantic/core-ddd/compare/v5.0.1...v6.0.0) (2026-11-14)

### ⚠ BREAKING CHANGES

* core-ddd now depends on eQuantic.Core.Data 7.0, whose repositories change what some existing calls do (a duplicate key surfaces as DuplicateKeyException at commit, among others). Applications get that behaviour through core-ddd, and must reference the 7.0 line of eQuantic.Core.Data and its EF family.

### Features

* move to eQuantic.Core.Data 7.0 and publish through trusted publishing ([#3](https://github.com/eQuantic/core-ddd/issues/3)) ([9d05ae6](https://github.com/eQuantic/core-ddd/commit/9d05ae6d04663141ece0c45131f0353289c85eb8))

## [5.0.1](https://github.com/eQuantic/core-ddd/compare/v5.0.0...v5.0.1) (2026-07-25)

### Bug Fixes

* resolve build warnings and refresh the README for v5.0.0 ([31e5451](https://github.com/eQuantic/core-ddd/commit/31e5451f34ff0ea08afca9b989a85692bbcfd24c))

## [5.0.0](https://github.com/eQuantic/core-ddd/compare/v4.0.0...v5.0.0) (2026-07-24)

### ⚠ BREAKING CHANGES

* EntityBase<TKey> now compares by identity instead of by reference, so
sets, dictionaries and Distinct() treat two instances with the same key as one entity.

### Features

* tactical DDD blocks and the domain-persistence seam ([d28be9f](https://github.com/eQuantic/core-ddd/commit/d28be9f17f9d7aa7b0723f7677e7aa35cdd2abed))

### Bug Fixes

* **build:** drop the sibling-repo project from the solution ([7184d84](https://github.com/eQuantic/core-ddd/commit/7184d841168ebc80685e7b1844adf212bbde99a2))
