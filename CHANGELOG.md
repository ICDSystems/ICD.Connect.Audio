# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
 - Added Shure MXWAPT4 support

## [9.2.0] - 2019-07-16
### Changed
 - Better handling of persistent connector info for telemetry
 - PowerOn/PowerOff methods changed to support new pre-on/off callbacks

## [9.1.1] - 2019-07-26
### Changed
 - Fixed null reference issues when a Biamp TI control is instantiated without a hold control

## [9.1.0] - 2019-05-15
### Added
 - Added telemetry features to switcher devices
 - Added telemetry features to Biamp

## [9.0.3] - 2019-05-01
### Changed
 - Fixed bug where Biamp Tesira TX/RX would become desynchronized during initialization

## [9.0.2] - 2019-04-10
### Changed
 - Fixed typo that was preventing QSysCore from loading BooleanNamedControls

## [9.0.1] - 2019-04-04
### Changed
 - Fixed errant feedback issue with Biamp Tesira partition events

## [9.0.0] - 2019-01-02
### Added
 - Added microphone control interface and abstraction
 - Added Volume Feedback Log
 
### Changed
 - Moved controls into subdirectories/namespaces
 - Inserted AbstractVolumeDeviceControl into volume control heirarchy
 - Biamp doesn't clear queue on port disconnect (queue already does itself)

## [8.2.0] - 2018-11-20
### Added
 - Added missing enum for Biamp Tesira VOIP prompt

### Changed
 - Fixed bad validation that was breaking Biamp Tesira feedback
 - Fixed typo in QSys configuration parsing
 - Improved Biamp Tesira POTS call performance
 - Fixed potential memory leak in Biamp Tesira feedback subscription

## [8.1.0] - 2018-11-08
### Added
 - Added Misc project and BiColorMicButton device

## [8.0.0] - 2018-10-30
### Added
 - Added DenonAVR project and device driver

### Changed
 - Small optimizations to TesiraTextProtocol deserialization
 - Fixed potential multi-enumeration
 - Fixed loading issues where devices would not fail gracefully if a port was missing

## [7.0.0] - 2018-10-18
### Added
 - Dialers support booking dialing

### Changed
 - Biamp now sets call appearance properly when adding/removing sources
 - MXA310 Format Exception bug fix. Fixed regex for Sample response types.

## [6.1.0] - 2018-10-04
### Added
 - Added shim for setting ShureMXA LED brightness and colour at the same time
 
### Changed
 - MockAudioDeviceVolumeControl better implements new volume control features

## [6.0.0] - 2018-09-25
### Added
 - Positional volume ramping

### Changed
 - Untangled volume control inheritance

## [5.4.0] - 2018-09-14
### Added
 - MXA microphones report mute button state

### Changed
 - Significant optimizations

## [5.3.0] - 2018-07-19
### Changed
 - No longer choking completely when a control loads with partially valid data
 - QSys Core named controls query initial values on initialization
 - ThinConferenceSource SourceType must now be specified

## [5.2.1] - 2018-07-02
### Changed
 - Potential fix for bug where Biamp calls would lose caller information

## [5.2.0] - 2018-06-19
### Added
 - Added distinct call reject method

### Changed
 - Using new conferencing interfaces
 - QSys feedback improvements on reconnect

## [5.1.0] - 2018-06-04
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
 