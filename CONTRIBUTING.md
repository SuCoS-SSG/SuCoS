# Contributing to SuCoS

Thank you for your interest in contributing to **SuCoS**! Please take a moment to review this document **before** submitting a merge request.

Before we embark on this exciting journey, let's take a moment to appreciate the heart and soul of SuCoS: its [GitLab repository](https://gitlab.com/sucos/sucos). The repository is a treasure trove of SuCoS's source code, issues, and discussions. Feel free to explore, contribute, and make SuCoS even more awesome!

We hope you're as excited as we are about the possibilities **SuCoS** brings to the world of static site generation. Let's embark on this journey together and unleash the true potential of static websites. Happy coding and creating!

## Merge Requests

Please ask first before starting work on any significant new features.

To prevent unnecessary effort, we request that contributors create a feature request to first discuss any new ideas. Your ideas and suggestions are welcome!

Please ensure that the tests are passing when submitting a merge request. If you're adding new features to **SuCoS**, please include tests.

## Where do I go from here?

For any questions, support, or ideas, etc. please create a GitLab issue. If you've noticed a bug, please submit an issue.

### Fork and create a branch

If this is something you think you can fix, then fork **SuCoS** and create a branch with a descriptive name.

### Check if the Test Suite is working

There are several tests done on each commit. The whole test suite currently take seconds.

Linux:

```sh
\build.sh test
```

Windows:

```powershell
\build.ps1 test
```

### Implement your fix or feature

At this point, you're ready to make your changes. Feel free to ask for help.

Run the test suite again to certify that everything passes. Feel free to change, add or enhance the tests themselves. It also will show you how much of the whole code is tested. When adding a new feature, it's commum

### Create a Merge Request

At this point, if your changes look good and tests are passing, you are ready to create a merge request.

GitLab CI will run our test suite against all supported environments. It's possible that your changes pass tests in one environment but fail in another. In that case, you'll have to setup your development environment for the problematic environment, and investigate what's going on.

## Merging a MR (maintainers only)

A PR can only be merged into master by a maintainer if: CI is passing, approved by another maintainer (if there are more than one) and is up to date with the default branch. Any maintainer is allowed to merge a MR if all of these conditions are met.

## Shipping a release (maintainers only)

Maintainers need to do the following to push out a release:

* Create a feature branch from master and make sure it's up to date.
* Review and merge the PR.

There is a scheduled that checks new code every Thursday. If it does, it will create a new tag, new GitLab release and generate all the files needed.

> Attention: when executing a pipeline manually on main, it will also trigger the code verifier and might create a new release.
