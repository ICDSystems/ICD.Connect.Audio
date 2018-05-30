# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Changed
 - Fixed bug that was preventing Biamp Tesira devices from loading
 - Serial devices use ConnectionStateManager for maintaining connection to remote endpoints

## [5.0.0] - 2018-05-24
### Added
 - QSys Core VoIP dialer implementation

## [4.0.0] - 2018-05-09
### Added
 - Added ICD.Connect.Audio.Mock project
 - Adding MockAudioDevice
 - Adding API attributes for volume controls
 - Adding console features for volume controls
 - Adding console features to volume control abstractions
 - Adding Shure MXA source routing control
 - Moved volume controls, volume points, volume repeaters and more from other projects

### Changed
 - Updated to Callbacks for thinconferencesource

## [3.1.0] - 2018-05-02
### Added
 - Biamp and QSys DSPs now have route source/destination controls
 - Added ICD.Connect.Audio project
 - Added GenericAmpDevice and settings

### Changed
 - Biamp TI conference sources now reflect correct hold state
 - Biamp TI dialer leaves hold state between calls
 - Biamp TI dialer correctly rejects incoming calls
 - Biamp and QSys DSPs dispose only dynamically loaded controls on clear.

## [3.0.1] - 2018-04-27
### Changed
 - Biamp Tesira properly updating caller name/number for POTS

## [3.0.0] - 2018-04-27
### Added
 - Adding 10MB and 100MB enums to Biamp Tesira Link Status
 - Adding Biamp Tesira method for parsing unknown values to a default C# object

### Changed
 - Fixing parsing bug when the QSys would send concatenated JSON data.
 - Removed suffix from assembly names
 