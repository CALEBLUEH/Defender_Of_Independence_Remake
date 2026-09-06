// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Fungus
{
    /// <summary>
    /// Static class that hooks into the hierachy changed and item gui callbacks to put
    /// a fungus icon infront of all GOs that have a flowchart on them
    /// 
    /// Reference; http://answers.unity3d.com/questions/431952/how-to-show-an-icon-in-hierarchy-view.html
    /// 
    /// TODO
    /// There is what appears like a bug but is currently out of our control. When Unity reloads the built scripts it fires
    /// InitializeOnLoad but doesn't then fire HierarchyChanged so icons disappear until a change occurs
    /// </summary>
    [InitializeOnLoad]
    public class HierarchyIcons
    {
        // the fungus mushroom icon
        static Texture2D TextureIcon { get { return Fungus.EditorUtils.FungusEditorResources.FungusMushroom; } }

        // Cached identifiers for GameObjects that have flowcharts on them.
#if UNITY_6000_5_OR_NEWER
        static HashSet<EntityId> flowchartIDs = new HashSet<EntityId>();
#else
        static List<int> flowchartIDs = new List<int>();
#endif

        static bool initalHierarchyCheckFlag = true;

        static HierarchyIcons()
        {
            initalHierarchyCheckFlag = true;
#if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyIconCallback;
#else
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyIconCallback;
#endif
#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += HierarchyChanged;
#else
            EditorApplication.hierarchyWindowChanged += HierarchyChanged;
#endif
        }

        //track all gameobjectIds that have flowcharts on them
        static void HierarchyChanged()
        {
            flowchartIDs.Clear();

            if (EditorUtils.FungusEditorPreferences.hideMushroomInHierarchy)
                return;

        #if UNITY_6000
            var flowcharts = GameObject.FindObjectsByType<Flowchart>(FindObjectsSortMode.None);
        #else
            var flowcharts = GameObject.FindObjectsOfType<Flowchart>();
        #endif

        #if UNITY_6000_5_OR_NEWER
            flowchartIDs = flowcharts.Select(x => x.gameObject.GetEntityId()).ToHashSet();
        #else
            flowchartIDs = flowcharts.Select(x => x.gameObject.GetInstanceID()).Distinct().ToList();
            flowchartIDs.Sort();
        #endif
        }

        // Draw the icon if the object identifier is in our cache.
#if UNITY_6000_5_OR_NEWER
        static void HierarchyIconCallback(EntityId entityId, Rect selectionRect)
#else
        static void HierarchyIconCallback(int instanceID, Rect selectionRect)
#endif
        {
            if(initalHierarchyCheckFlag)
            {
                HierarchyChanged();
                initalHierarchyCheckFlag = false;
            }

            if (EditorUtils.FungusEditorPreferences.hideMushroomInHierarchy)
                return;

            // place the icon to the left of the element
            Rect r = new Rect(selectionRect);
#if UNITY_2019_1_OR_NEWER
            r.x -= 28;  //this would make sense as singleLineHeight *2 but it isn't as that includes padding
#else
            r.x = 0;
#endif
            r.width = r.height;

            //GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            //binary search as it is much faster to cache and int bin search than GetComponent
            //  should be less GC too
#if UNITY_6000_5_OR_NEWER
            if (flowchartIDs.Contains(entityId))
#else
            if (flowchartIDs.BinarySearch(instanceID) >= 0)
#endif
                GUI.Label(r, TextureIcon);
        }
    }
}
