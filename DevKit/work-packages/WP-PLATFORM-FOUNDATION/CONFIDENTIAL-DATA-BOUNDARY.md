# Confidential Data Boundary

## Scope

Apparent personnel and business-data artifacts present in the local checkout are
confidential local inputs and are outside the platform-foundation work package.
They must not be inspected, parsed, copied, moved, renamed, deleted, staged,
committed, documented by name, or used to infer product behavior.

Only synthetic generated data may be used for fixtures, tests, screenshots,
seeding, demonstrations, or development bootstrap.

## Git finding

The checked excluded artifacts are untracked and ignored locally. None are
tracked by Git. Their presence does not authorize their use. Existing ignore
patterns are not extended or relied upon as a data-governance control.

## Engineering boundary

Implementation may use source code, project files, configuration, migrations,
tests, and existing documentation. If confidential content is found embedded in
a source-controlled file that must be modified, work stops for owner review.

Runtime import storage is also opaque. Import framework tests must create their
own synthetic files in isolated temporary directories and clean them up.

