# Changelog

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
