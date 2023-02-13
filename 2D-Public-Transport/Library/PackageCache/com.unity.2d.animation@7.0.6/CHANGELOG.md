# Changelog

## [7.0.6] - 2022-05-12
### Fixed
- Fix Sprite Library Asset multi-editing in the Inspector window. (case 1417747)

### Changed
- Updated Visibility Tab documentation page.
- Removed the 'Open' button from Sprite Library Asset and Sprite Library Source Asset inspectors.

## [7.0.5] - 2022-03-21
### Fixed
- Fixed an issue where moving vertices in the Skinning Editor might result in invalid edges. (case 1402637)
- Sprite Library cannot reference itself in the Main Library field or any asset that references it. (case 1402645)
- Fixed an issue where the SpriteSkin components would not get initialized on editor launch. (case 1403569)
- Fixed an issue when Skinning Editor will lose reference after exiting Play mode. (case 1405327)
- Fixed an issue where the Skinning Editor's copy/paste tool would fail if two bones shared the same name. (case 1411596)

## [7.0.4] - 2022-02-07
### Fixed
- Deleting certain vertices in sprite mesh doesn't leads to mesh being resetted to quad. (case 1386411)
- Fixed an issue where the animation window's preview of IK targets would not be the same as in Play Mode. (case 1396082)

## [7.0.3] - 2021-11-24
### Fixed
- Sprite selection now reacts only to the left mouse button. (case 1378439)
- Added additional fallback for when the bursted tessellation step fails. (case 1382979)
- Fixed an issue where a Sprite Skin outside of the camera frustum enters the frustum, which could cause an editor and player crash. (case 1380228)
- Sprite Skin's help button now leads to the correct documentation page. (case 1383768)

## [7.0.2] - 2021-11-05
### Fixed
- Fixed errors in the Sprite Skin section of documentation. (case 1368100)
- Quads are now generated at correct positions after removing all vertices from Sprite meshes. (case 1369724)
- Fixed an issue where removing an edge in the Skinning editor would result in the mesh falling back to a quad. (case 1369516)

## [7.0.1] - 2021-09-21
### Fixed
- Fixed render texture size error in the Skinning Editor. (1357814)
- Fixed a case where quads generated in the Skinning Editor would be created with the wrong size and position. (case 1361500)
- Fixed an issue where IK Solvers would not be updated when previewing an animation clip. (1364307)
- Fixed an issue where copying mesh and bone data from a .psb containing a single sprite would throw an exception. (case 1364619)
- Fixed an issue where an error would show up when destroying a Sprite Skin component while deep profling. (case 1364916)

## [7.0.0] - 2021-08-10
### Fixed
- Bone and Sprite influence lists are displayed correctly (case 1349041)
- Exception thrown when manually adding vertices in the Skinning Editor to a Sprite w/o mesh. (case 1348202)
- Sprite outline in the Skinning Editor is now rendered based on Sprite's geometry. (case 1354798)

## [7.0.0-pre.3] - 2021-07-05
### Fixed
- Thumbnails in Sprite Library Asset flicker when the Library Asset contains many Categories and Labels. (case 1333228)
- SpriteLibraryAsset Category & Label does not generate hash in Inspector. (case 1340587)
- SpriteResolver inspector selects first element when failing to resolve. (case 1340070)
- IKManager2D does not detect classes inheriting from Solver2D. (case 1343260)
- Skinning Editor tooltips updated.

## [7.0.0-pre.2] - 2021-05-19
### Fixed
- Fixed Sprite Resolver component not updated when new categories/labels are added into Sprite Library asset. (case 1321069)

## [7.0.0-pre.1] - 2021-05-05
### Changed
- Version bump for Unity 2021.2.
- Moved Copy & Paste Rig buttons in the Skinning Editor into their own Rig category.
- Moved Preview & Restore Pose button in the Skinning Editor into their own Pose category.
- Replaced Triangle.Net with our own tessellation solution.

### Added
- Added shortcuts at the back of the Skinning Editor buttons tooltips for better discoverability.
- Added a color picker for each bone in the Skinning Editor's Visiblity tab.
- Added the Sprite Influence panel that allows to change a bone influence on the selected Sprite.

### Fixed
- Fixed crash when disabling Sprite Skin when multithreaded rendering is enabled. (case 1296355)

## [6.0.0] - 2021-03-17
### Changed
- Update version for release

## [6.0.0-pre.3] - 2021-03-15
### Changed
- Updated manual

### Fixed
- Deleting bones from a skeleton referenced by another character sometimes throws IndexOutOfRangeException (case 1304768)

## [6.0.0-pre.2] - 2021-01-16
### Changed
- Update license file

### Added
- Initial documentation update for new features for 6.0.0

### Fixed
- SpriteResolver resets to the current Sprite from SpriteRenderer if exist in Sprite Library

## [6.0.0-pre.1] - 2020-11-02
### Changed
- Sprite Swap related features moved out of experimental namespace
- Removed editing of Sprite Swap feature in Skinning Module
- Updated Sprite Swap workflow focusing on Project Window and Inspector
- Sprite Library Asset is created via AssetImporter

### Added
- Sprite Library Asset supports variant
- Sprite Library supports override during editing
- Supports sharing of bone structures in Skinning Module
- Added position, rotation and bone color editing in Skinning Module

### Fixed
- Added missing tooltips in the Sprite Skin inspector (case 1285255)

## [5.0.3] - 2020-10-15
### Fixed
- Fixed Sprite with no animation data is being processed during AssetPostProcessor
- Fixed properties under the Sprite Library Asset overlapping in inspector (case 1280017)
- Fixed vertical slider handle is not aligned and placed slightly to the right side in the Bone Influence window (case 1260568)

## [5.0.2] - 2020-08-31
### Fixed
- Fixed Visibility window overlaps with weights and geometry window when Sprite Editor Window resizes (case 1263353)
- Fixed 'Depth' column label gets clipped in Visibility Tool Window (case 1257991)
- Fixed 'Invalid worldAABB' error message when repeatedly pressing Pack Preview button (case 1270150)
- Fixed Null reference exception when changing values of a material while recording animation with Skinning Module enabled (case 1267300)
- Improved memory and speed of Animation SpritePostProcess for large sprite count

## [5.0.1] - 2020-07-24
### Fixed
- Fixed Skinning module flickers when creating in category in Visibility Window (case 1244097)
- Fixed NullReferenceException when creating Preset for SpriteSkin component (case 1254873)
- Updated optional dependency support for Collections to 0.9.0-preview.6 and Burst 1.3.3 (case 1255839)

## [5.0.0] - 2020-05-11
### Changed
- Version bump for Unity 2020.2

### Added
- Combined 2D IK package with 2D Animation package

### Fixed
- Remove unused XR dependency (case 1249390) 
- Fixed NullReferenceException when creating prefab with SpriteSkin component (case 1245149)

## [4.2.4] - 2020-05-19
### Fixed
- Fixed compilation error by depending on latest 2D Common package.

## [4.2.3] - 2020-04-26
### Changed
- Improved performance by batching buffer submission when Collection and Burst package is installed

### Fixed
- Fix 'ArgumentException' when creating UXML Template Asset with 2D Animation package installed (case 1239680)

## [4.2.2] - 2020-03-26
### Fixed
- Fixed bone name field in weight slider does not display bone name (case 1226249)
- Fixed SpriteResolver's Inspector not updated when GameObject is disabled

### Changed
- Improved deformation performance when Collection and Burst package is installed

### Added
- Allow reordering of bone order in Bone Influence window. This is to allow fine tuning of bone order when shown in SpriteSkin's Inspector

## [4.2.1] - 2020-03-20
### Fixed
- Fixed inconsistent line ending

## [4.2.0] - 2020-03-11
### Added
- Add alwaysUpdate option to SpriteSkin to determine if SpriteSkin execution should occur even when the associated SpriteRenderer is culled
- Added message to inform user on dependent packages when viewing certain sample Scenes
- Added API to access deformed vertices from SpriteSkin

### Changed
- Improved SpriteSkinEditor UI
- Adjust length of popup and value fields for Weight Slider Window

### Removed
- Remove Bounds Gizmo from SpriteSkin
- Remove Reset Bounds button from SpriteSkinEditor

### Fixed
- Fixed Sprite asset used by SpriteSkin in Scene is being deleted
- Fixed broken documentation links in inspectors
- Fixed Sprite deformation not updated when GameObject is being enabled
- Fixed exception after reverting from creating new vertices and edges
- Fixed visual defect after undoing changes to Bone Transform properties in SpriteSkin's Inspector

## [4.1.1] - 2020-01-20
###Fixed
- Fix 2D Animation not working when reloading scene in runtime (case 1211100)

###Added
- Bone visibility persist after apply
- Sprite visibility persist after apply

###Changed
- Deformed Sprite's bounds are now calculated and bounds property is removed from SpriteSkin's inspector (case 1208712)

## [4.1.0] - 2019-12-18
### Changed
- Changed default shortcut key for "Animation/Create Vertex" from "Shift-D" to "Shift-J"
- Changed default shortcut key for "Animation/Weight Brush" from "Shift-C" to "Shift-N"

### Fixed
- Fix visual glitch when using SpriteSwap with Multi-threaded rendering (case 1203380)
- Fix bone name misaligned under Weight Slider Inspector when a name contains more than 26 letters (case 1200873)
- Fix bones not chained correctly when splitting bone in certain cases
- Fix 'Label' and 'Sprite' name overlaps with its input field when preset of "Sprite Library Asset" is created (case 1201061)
- Fix bone names can be empty (case 1200861)
- Fix bone gets created even though clicked on Visibility Panel (case 1200857)
- Fix NullReferenceException when using shortcut 'Shift+1' in certain cases (case 1200849)

### Added
- Expose SpriteSkin component to be accessible from scripts.

## [4.0.1] - 2019-11-26
### Changed
- Changed how Samples are imported into the user's project
- Updated Third Party Notices file

### Fixed
- Fix Animation Samples crashes when installing on certain machines (case 1185787)

## [4.0.0] - 2019-11-06
### Changed
- Version bump for Unity 2020.1
- Improve optional performance boost by installing Burst and Collections package. Currently tested with 
    - com.unity.collections 0.1.1-preview
    - com.unity.burst 1.1.2

### Added
- Skinning Module now persists the following state after Apply or Revert is clicked in Sprite Editor Window
    - Current view mode i.e. Character or Spritesheet Mode
    - Sprite Selection
    - Bone Selection
    - Preview Pose
    - Vertex Selection
    - Visibililty Tool Active State
    - Weight Brush Settings

### Fixed
- Update to use new UIElements ListView API

## [3.0.7] - 2019-10-18
### Fixed
- Fix Search reset button is visible in Visibility tool when nothing is entered into the search field (case 1182627)
- Fix Sprite outline disappears when the Selected Outline Color alpha value is less than 255 (case 1186776)
- Fix button's label spelling error in 'Generate For All Visible' (case 1188621)

## [3.0.6] - 2019-09-18
### Changed
- Remove usage of Resource folder for assets needed by package.

### Fixed
- Fix GC allocation when using Sprite Resolver.

## [3.0.5] - 2019-09-06
### Added
- Optional performance boost by installing Burst package.
- Samples showing different how to produce different outcomes

### Changed
- Sprite and Group in Sprite Visibility Window appear in same order as original art source file

### Fixed
- Fix missing bone data in Sprite when importing with AssetDatabase V2

## [3.0.4] - 2019-08-09
### Added
- Add related test packages
- Added tangent deform for lighting support

### Fixed
- Fixed Amount slider not working in Weight Slider Panel
- Fixed exception when changing size to less than 0 in SpriteLibraryAssetInspector
- Fixed Sprite visual corruption when swapping Sprite using SpriteResolver

###Changed
- Make Size property field in Weight Brush draggable for changing brush size
- Rename SpriteLibraryAsset::GetCategorylabelNames to SpriteLibraryAsset::GetCategoryLabelNames
- Change string hash for Category and Label name. This might break existing animation usage with SpriteResolver.
- Add Experimental tag on Sprite Swap related features


## [3.0.3] - 2019-07-17
### Changed
- Update documentation
- Update to latest Mathematics package version

## [3.0.2] - 2019-07-13
### Changed
- Mark package to support Unity 2019.3.0a10 onwards.

## [3.0.1] - 2019-07-12
### Changed
- Fix path length due to validation failure.

## [3.0.0] - 2019-06-17
### Changed
- Remove preview tag.
- Remove experimental namespace

## [2.2.0-preview.3] - 2019-06-06
### Added
- BoneGizmos can be toggled in the SceneView Gizmos dropdown
- Scrollbar does not show for toolbar when required to scroll
- Change Sprite Library implementation.
 - APIs redesigned. This will cause compilation errors if previous APIs are used
 - Data serialization change. Previous Asset and Component data will not be compatible

## [2.2.0-preview.2] - 2019-05-10
### Added
- BoneGizmos will only show in selected hierarchies.
- Associate nearest Bone to Sprite intead of root Bone when no Bones are overlapping the Sprite
- Fixed Sprite not showing after it is being culled due to bone animation
- Add icons for Sprite Library Asset, Sprite Library Component and Sprite Resolver Component
- Fixed Sprite Library Asset Inspector Property Field text clipping
- SpriteResolver will assign Sprite to SpriteRenderer even it resolves to a missing Sprite
- Add visual feedback in SpriteResolver Inspector for missing Sprite

## [2.2.0-preview.1] - 2019-05-09
### Added
- Upgrade for 2019.2
- Copy and Paste rework
- Visibility Window remains open when switching between tools
- Reparent Bone tool removed and functionality moved into Bone Visibility Panel
- Added Sprite Library feature
- Add Layer Grouping support in Sprite Visibility Panel

## [2.1.0-preview.4] - 2019-04-29
### Added
- Fix skinning not in sync with the rendering.

## [2.1.0-preview.3] - 2019-04-24
### Added
- Set Burst compilation off for internal build

## [2.1.0-preview.2] - 2019-02-25
### Added
- Fix enable skinning on add SpriteSkin component
- Upgrade dependency package version for Unity 2019.1 support
- Fix case 1118093: SpriteSkin.onDrawGizmos() increases memory usage.

## [2.1.0-preview.1] - 2019-01-25
### Added
- Update package to work with 2019.1
- Improve animation runtime performance
- Fix bone reparenting sibling order
- Fix Sprite Visibility Tool in disabled state in certain cases
- Update documents

## [2.0.0-preview.1] - 2018-11-20
### Added
- Overhauled 2D Animation workflow.
  - Refer to updated documentation for workflow changes.
- Single Sprite Editor Window module for 2D Sprite Rigging workflow
  - Unified Bone, Geometry and Weight tools in a single window
- Supports Multiple Sprite Single Character rigging workflow through 2D PSD Importer Package.
- SpriteSkin now uses user define bounds for renderer culling

## [1.0.16-preview.2] - 2018-11-14
### Added
- Fix 2 Issues:
  1. Prefabs with SpriteSkin loses references to bone hierarchy when Library folder is rebuilt/different.
  2. The scene viewport shows the character without any bones applied, needing an re-import.

## [1.0.16-preview.1] - 2018-07-18
### Added
- Fix error log about VertexAttribute

## [1.0.16-preview] - 2018-06-20
### Added
- Fix Documentation warnings
- Fix error log complaining about DidReloadScripts signature.
- Fix issues with generate outline

## [1.0.15-preview] - 2018-04-12
### Added
- New Version suffix (preview)
- Improved Scene View gizmos for better manipulation of bone rotation and position
- Added notification when Sprites are imported with incorrect weights
- Fixed bug where textures with max texture size could not generate geometry

