# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Changed
 - VolumeRepeater now properly ramps percent volume representations

## [15.0.0] - 2021-01-14
### Added
 - Biamp Parle microphone device
 - Biamp Tesira now ignores Echoed responses, supporting SSH and allowing more resilient connections

### Changed
 - Split IBiColorMicButton interfaces into IBiColorMicLed & IMicMuteButton. Created interface that implements both: IBiColorMicButtonLed
 - Biamp Tesira - Move common MonitoredDeviceInfo telemetry to BiampTesiraTelemetryComponent

## [14.1.3] - 2020-09-24
### Changed
 - Fixed a bug where default audio activities were not being initialized

## [14.1.2] - 2020-09-08
### Changed
 - Fixed a bug where malformed QSys JSON was not being logged

## [14.1.1] - 2020-08-13
### Changed
 - Fixed a regression where Biamp subscription responses were not being paired correctly

## [14.1.0] - 2020-07-14
### Changed
 - Improved volume telemetry, differentiating between level and percentage
 - Fixed a bug where Biamp serial commands were not being safely compared

## [14.0.0] - 2020-06-18
### Added
 - UUIDs are loaded from DSP configs

### Changed
 - MockAudioDevice now inherits from AbstractMockDevice
 - Refactoring for telemetry, logging, device controls, and more

## [13.1.0] - 2020-06-10
### Changed
 - Implemented StartSettings to start communications with devices

## [13.0.0] - 2020-05-23
### Changed
Changed Biamp and QSys conferecing for ConferenceHistory changes - StartTime/EndTime renames, AutoAnswer rename, incoming calls have no direction

## [12.1.0] - 2020-03-24
### Added
 - VolumePointHelper - Added OnVolumeControlSupportedVolumeFeaturesChanged event

### Changed
 - Fixed a bug where Volume Safety Max was not being correctly enforced
 - Fixed a bug where volume percentages were not being serialized correctly from DeployAV
 - Fixed volume percentage string representations in console

## [12.0.0] - 2020-03-20
### Added
 - Added context enum to IVolumePoint and implementations
 - Added mute type enum to IVolumePoint and implementations
 - Added privacy mute volume control support to QSys Core and Biamp Tesira drivers
 - Conference controls implement SupportedConferenceFeatures property

### Changed
 - Abstract Traditional Participant property setters are now protected so we set them via methods instead of by changing the property.
 - Complete rewrite of existing volume controls
 - Simplification of volume ramping for percent vs level
 - Volume ramping configuration moved into volume points (step size, interval, safety range)
 - Fixed bug where QSys camera was reporting the wrong number of supported presets
 - Failing gracefully when a Biamp control fails to load
 - Using UTC for times

## [11.7.2] - 2020-02-25
### Changed
 - Fixed a bug where Biamp RoomCombiner volume controls were not pulling the right min/max range
 - Fixed issue loading Biamp partitions with bad Channel configuration

## [11.7.1] - 2020-02-21
### Changed
 - QSys Core camera device implements StoreHome method

## [11.7.0] - 2020-02-20
### Added
 - QSys Core camera device supports returning to home position and privacy mute

## [11.6.2] - 2020-02-07
### Added
 - Additional QSys Core logging for POTS and VoIP calls
 - Added console features for examining the contents of a QSys Core ChangeGroup

### Changed
 - Fixed a Null Reference related to VoIP/POTS calls on QSys Core
 - Fixed a bug where reloading QSys Core controls would not properly unload old controls
 - Fixed a bug where QSys Core POTS named components were not reporting state changes

## [11.6.1] - 2019-12-09
### Changed
 - Changed QSys Configuration of NamedControls to use elements instead of attributes, to match DAV generated config
 - Changed QSys Configuration of ChangeGroups to use elements instead of attributes, for consistency

## [11.6.0] - 2019-11-18
### Added
 - Added driver for Shure Mx396

### Changed
 - Moved Shure microphone devices into subdirectories

## [11.5.1] - 2019-10-25
### Changed
 - AbstractVolumePoint inherits from new AbstractPoint type

## [11.5.0] - 2019-09-16
### Added
 - Added ClockAudio CCRM4000 device

### Changed
 - Implementing new PowerState for Denon receiver
 - API changes to audio controls

## [11.4.0] - 2019-08-15
### Added
 - Add Shure MXWAPT2 and MXWAPT8 devices

## [11.3.0] - 2019-07-10
### Added
 - Biamp Tesira and QSys Core implement partition SupportsFeedback properties

## [11.2.1] - 2019-07-10
### Changed
 - QSys Core fails more gracefully when the config path does not exist
 - Fixed a bug where Biamp Tesira VoIP was not properly handling incoming calls

## [11.2.0] - 2019-05-01
### Added
 - QSys Core Camera support
 - QSys Core POTS support
 - QSys Core snapshot support

### Changed
 - Significant QSys Core refactoring
 - Fixed bug where QSys Core would attempt to send changegroup data before initialization

## [11.1.0] - 2019-03-27
### Added
 - Added QSys Core partition control

### Changed
 - Significant refactoring to QSys control loading

## [11.0.0] - 2019-01-14
### Changed
 - Dialing features refactored to fit new conferencing interfaces

## [10.0.0] - 2019-01-10
### Added
 - Added port configuration features to audio devices

## [9.7.0] - 2020-04-30
### Changed
 - Fixed NullRefException with BiColorMicButtons
 - IVolumePositionDeviceControl - added range attributes to VolumePosition
 - BiampExternalTelemetryProvider - Changed ActiveFaults to ActiveFaultState
 - BiampExternalTelemetryProvider - Fixed ActiveFaultsState parsing to handle no device faults correctly
 
### Added
 - BiampExternalTelemetryProvider - added network info telemetry
 - BiampExternalTelemetryProvider - added serial number telemetry
 - BiampExternalTelemetryProvider - added active fault message telemetry
 
### Removed
 - BiampExternalTelemetryProvider - removed VoIP and CallControl telemetry - it did not properly handle multiple cards

## [9.6.0] - 2020-02-18
### Changed
 - Fixed issue with Biamp Tesira VoIP AutoAnswer Set commands
 - Fixed Biamp Tesira handling of unsolicited feedback
 - Added VoIp prompt values "Unknown" and "Service Unavaliable"

## [9.5.1] - 2020-02-14
### Changed
 - Improvements to Biamp Serial Queue

## [9.5.0] - 2020-02-05
### Added
 - IBiColorRelayMicButton as an interface for all bicolor mic buttons
 - BiColorRelayMicButtonDevice to support microphones using relays instead of I/O ports
 
### Changed
 - Refactored BiColorMicButtonDevice to an abstract supporting multiple mic button devices

## [9.4.1] - 2019-12-06
### Added
 - Handling for "INVALID VoIPRegStatus" state to Biamp Tesira Voip RegistrationState

## [9.4.0] - 2019-11-18
### Added
 - Ability to modify increment value for AbstractVolumeLevelDeviceControl

### Changed
 - Fixed RepeatBeforeTime and RepeatBetweenTime for AbstractVolumeLevelDeviceControl to affect proper repeater

## [9.3.2] - 2019-12-12
### Added
 - Added handling of Tesira VoIP Invalid registration state

## [9.3.1] - 2019-07-31
### Changed
 - Re-added White LED enum to Shure mics
 - Biamp Tesira POTS privacy mute and do-not-disturb controls are optional

## [9.3.0] - 2019-07-26
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
 