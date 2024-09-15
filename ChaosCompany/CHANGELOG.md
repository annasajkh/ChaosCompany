# Change Log
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).


## [1.1.4] - 9/15/2024

Change and fixed a bunch of stuff

### Changed
- Enemy spawning sync with the time multiplier
- Now it's possible for all mod enemy to actually spawn
- Remove chaotic item because sometimes it just break the game and i can't fix it

### Fixed
- Fix move enemy now it should fucking work 100%
- Fix Spawn rate doesn't actually change lol

## [1.1.3] - 9/12/2024

globalTimeSpeedMultiplier changes

### Changed
- Changed globalTimeSpeedMultiplier random range to 0.25 - 1.75

## [1.1.2] - 9/7/2024

Minor readme changes

### Changed
- Forgot to include someone to the readme

## [1.1.1] - 9/7/2024

Minor chaotic item update

### Changed
- Rollback chaotic item changes

## [1.1.0] - 9/7/2024

Major update

### Changed
- Add move enemy


### Fixed
- Fix only the host can see the deadline as 3 instead of the mod picked random deadline for the first round
- Fix chaotic entities not despawning if you go to the same moon

## [1.0.3] - 9/4/2024

Tweak chaotic item

### Changed

- Change chaotic item random scrap value range to 5 - 150
- Change chaotic item spawn count to Random.Range(2, 5)

## [1.0.2] - 9/4/2024

Change chaotic item because it was too op

### Changed
  
- Reduce random scrap value range for chaotic item from 0 - 300 to 25 - 150
- Change chaotic item spawn count to Random.Range(5, 8)

### Fixed
- Fix chaotic item not setting scrap value at first time it spawn
- Fix chaotic item clipping through ground
- Fix chaotic item spawn rate not resetting after the first round

## [1.0.1] - 9/4/2024

Nothing lol because i accidentally upload it to thunderstore
 
## [1.0.0] - 7/4/2024
 
Initial release