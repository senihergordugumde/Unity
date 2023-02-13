# Sprite Visibility panel
Toggle the __Sprite Visibility__ panel by selecting the __Visibility tool__ button along the upper right of the editor window:

![](images/Highlighted_Visibility_icon.png)

The panel appears on the right-side of the editor window. It has two sliders at the top that control the visibility of the ![](images/bone_icon.png) bones and ![](images/mesh_icon.png) Sprite meshes within the editor window. Move either slider further to the left to decrease the visibility of the bones or meshes respectively, and to the right to increase their visibility.

![](images/2D-animation-v7-vis-panel.png)

The **Bone** tab displays the [Bone hierarchy](#bone-tab-and-hierarchy-tree) of the character Prefab, and the **Sprite** tab displays the names of the Sprites and their grouping hierarchy.

**Note:** You will no longer be able to create or define a [Sprite Library Asset](SLAsset.md) within the Skinning Editor module from 2D Animation version 6.0 onwards, and the **Category** and **Label** columns have been removed from the Sprite Visibility panel. Please refer to the [Sprite Library Asset documentation](SLAsset.md) for more information.

## Bone tab and hierarchy tree

![](images/bone_tree.PNG)<br/>The Bone tab selected.

Select the __Bone__ tab to view the list of bones in the character Prefab. The list reflects the hierarchy of bones you created with the [Bone tools](SkinEdToolsShortcuts.html#bone-tools). You can reparent and reorder bones directly from the bone tab by dragging selected bones up and down the list. Toggle the visibility of each bone by selecting the ![](images/visibility_icon.png) icon next to it.

| Property                        | Function                                                     |
| ------------------------------- | ------------------------------------------------------------ |
| ![](images/visibility_icon.png) | Toggle the visibility of each bone by selecting this icon next to the bone. |
| ![](images/visibility_icon.png) +Alt (macOS: +Option) | Toggle the visibility of a bone and its children by selecting this icon while holding Alt (macOS: holding Option). |
| __Bone__                        | The name of the Bone.                                        |
| __Depth__                       | Displays the Z-value of bones that are influencing the same Sprite Mesh. The parts of the Mesh that is influenced by a bone with higher **Depth** value will render in front of the Mesh influenced by bones with lower **Depth** value. <br/>A bone’s **Depth** value is 0 by default. |
| __Color__                        | The color of the Bone.                                        |

## Sprite tab

Select the __Sprite tab__ to see the list of Sprites that make up the character Prefab in the Skinning editor window. The names  and order of the Sprites mirror their names, layer and grouping order in the original source file. Toggle the visibility of a Layer by selecting the ![](images/visibility_icon.png) icon next to it. Hold Alt (macOS: hold Option) to view it in isolation and hide every other Layer.

![](images/2D-animation-v7-sprite-tab.png)
