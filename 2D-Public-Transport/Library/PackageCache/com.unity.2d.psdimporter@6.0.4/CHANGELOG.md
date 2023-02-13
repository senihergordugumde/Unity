# Changelog

## [6.0.4] - 2022-05-20
### Fixed
- Fixed Sprite missing reference on certain source file created by third party tools. (Case DANB-52)

## [6.0.3] - 2022-02-07
### Fixed
- Fixed unable to change mipmap settings in inspector. (Case 1381198)
- Fixed null exception when importing files with masks. (Case 1397354)

## [6.0.2] - 2021-11-24
### Fixed
- Fixed per platform settings does not get applied in Windows platform. (Case 1379970)
- Fixed unable to change mipmap settings in inspector. (Case 1381198)

## [6.0.1] - 2021-09-21
### Fixed
- Removed memory requirement check since we cannot properly determine if there will be enough memory to import the file (case 1357544)
- Fixed artifacts on images when flatten.
- Fixed exception "PsdInvalidException: Unrecognized layer section type" when importing certain files.

## [6.0.0] - 2021-08-10
### Fixed
- Fixed performance regression in PSDImporter Editor (case 1349408)
- Updated manual on Importer override to use latest API.

### Changed
- Changed some of PSDImporter settings to use checkboxes instead of drop down

## [6.0.0-pre.4] - 2021-07-05
### Fixed
- Fixed changing PSD Importer inspector fields causes UnityEditor.ObjectPreview errors (case 1333823)
- Fixed 2D PSDImporter always imports .psb files when switching build platforms (case 1327701)
- Fixed 2D PSDImporter doesn't apply settings from Sprite Editor Window when changes made in Inspector (case 1339799)

## [6.0.0-pre.3] - 2021-05-17
### Changed
- Update dependency version

## [6.0.0-pre.2] - 2021-05-14
### Changed
- Update dependency version


## [6.0.0-pre.1] - 2021-05-05
### Changed
- Version bump for Unity 2021.2

### Added
- Add ability to use Photoshop file's layer name to map to SpriteRect instead of using layer id
- Provide auto custom Physics shape generation option
- Add support to collapse layers in a Photoshop file's group
- Improve PSDImporter Inspector

### Fixed
- Fixed Unity hang when importing certain PSD files (case 1312835)

## [5.0.0] - 2021-03-17
### Changed
- Update version for release

## [5.0.0-pre.3] - 2021-03-15
### Changed
- Updated documentation

## [5.0.0-pre.2] - 2021-01-16
### Changed
- Update license file

### Fixed
- Fixed case 1291323 where upgrading from PSDImporter v2 causes Sprite to be missing

## [5.0.0-pre.1] - 2020-11-02
### Added
- Added bone sharing from other PSDImporter file

## [4.0.2] - 2020-08-31
### Fixed
- Fixed importing files with vector layers generates textures incorrectly (case 1266986)
- Fixed Sprite Editor Window doesn't show the Sprite when the Inspector is locked and the Sprite is not selected in the Project window

## [4.0.1] - 2020-07-07
### Fixed
- Fixed ArgumentException thrown when 2D Game Kit is imported for the first time (case 1244287)
- Updated to use non-experimental AssetImporter  (case 1254380)

## [4.0.0] - 2020-05-11
### Changed
- Version bump for Unity 2020.2

## [3.1.4] - 2020-04-09
### Fixed
- Fix PSD import issues with PSD file without unique layer id
- Fix crash on importing huge PSD files
- Fix metafile not updated when reimporting
- Fix error when importing PSB files with 32-bit color

### Changed
- Improve PSD file import performance

## [3.1.3] - 2020-03-20
### Changed
- Update 2D Animation dependency

## [4.0.0] - 2020-03-11
### Changed
- Version bump for Unity 2020.2

## [3.1.2] - 2020-02-27
### Fixed
- Fixed broken documentation links in inspectors
- Fixed empty GameObjects created in certain cases

## [3.1.1] - 2020-01-09
### Fixed
- Fix wrong dependency version

## [3.1.0] - 2019-12-16
### Added
- Expose PSDImporter class to be accessible via scripting
- Added example in manual to show how to set PSDImporter as default importer for PSD files.

## [3.0.0] - 2019-11-06
### Changed
- Update version number for Unity 2020.1
- Update documentation

## [2.0.6] - 2019-10-18
### Fixed
- Fixed SpriteRect name clash when Photoshop layer is renamed to the same name as an exisiting user created SpriteRect

## [2.0.5] - 2019-08-06
### Fixed
- Physics Shape not saved into Sprite when importing with AssetDatabase V2

### Added
- Experimental feature to have Sprites with same name generated from source file
- Support for providing Layer and Group order to Animation Skinning Module

## [2.0.4] - 2019-08-09
### Added
- Add related test packages
- Add support Secondary Texture Module in Sprite Editor Window

### Fixed
- Texture and SpriteLibraryAsset subassets in PSDImporter now follows the main asset's name.

## [2.0.3] - 2019-07-20
### Changed
- Update 2D Animation dependency

## [2.0.2] - 2019-07-13
### Changed
- Mark package to support Unity 2019.3.0a10 onwards.

## [2.0.1] - 2019-06-12
### Changed
- Update 2D Animation dependency

## [2.0.0] - 2019-06-17
### Changed
- Remove preview tag
- Remove experimental namespace

## [1.2.0-preview.2] - 2019-06-07
### Added
- Change API to internal access
- Only generate Sprite Library Asset if there is entry
- Do not reset Reslice checkbox after Inspector apply

## [1.2.0-preview.1] - 2019-03-15
### Added
- Update support for 2019.2
- Integrate with 2D Animation Sprite Library
- Integrate with new 2D Animation Character Group
- Fix asset name conflict

## [1.1.0-preview.2] - 2019-04-23
### Added
- Fix potential name clashing issues with ScriptedImporter
- Fix Prefab asset using wrong name. Note this will break Prefab references if upgrading from previous versions.

## [1.1.0-preview.1] - 2019-02-19
### Added
- Update dependency for 2019.1 support

## [1.0.0-preview.3] - 2019-02-19
### Added
- Fix compilation error in .NET 3.5

## [1.0.0-preview.2] - 2019-01-25
### Added
- Fix unable to rig Sprites created manually
- Remove legacy packing tag
- Default Texture Type is changed to 'Sprite (2D and UI)'
- Default Sprite Mode is changed to 'Multiple'

## [1.0.0-preview.1] - 2018-11-20
### Added
- New release
- ScriptedImporter for importing Adobe Photoshop file
- Supports handling of Adobe Photoshop layers
    - Creates Sprites from individual layers
    - Handles include or exclude hidden layers
- Supports Prefab generation that reconstruct generated Sprites to original art asset layout
    - Prefab generation supports GameObject grouping based on Adobe Photoshop layer grouping
- Supports 2D Animation v2 single character with multiple Sprites workflow
