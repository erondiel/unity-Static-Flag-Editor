// Static Check
// (c) 2024 Rodrigo Camacho / Mauricio Tonelli
// Public domain, do with whatever you like, commercial or not
// This comes with no warranty, use at your own risk!
// https://github.com/erondiel/unity-Static-Flag-Editor


using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq; // Ensure you have this for the ToList() method

public class NonStaticObjectsWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<GameObject, StaticEditorFlags> nonStaticObjects;
    private int objectNameWidth = 320; // Initial width for the GameObject name column
    private int flagToggleWidth = 75; // Initial width for each flag toggle column
    private bool showAllObjects = false; // False by default, showing only missing flags
    private bool isSortedAlphabetically = false;

    [MenuItem("Window/Level Design/Static Tag Editor")] // Add menu item to the Window menu
    static void Init()
    {
        NonStaticObjectsWindow window = (NonStaticObjectsWindow)EditorWindow.GetWindow(typeof(NonStaticObjectsWindow)); // Get existing open window or if none, make a new one:
        window.Show();
    }

    void OnGUI()
    {
        DrawDescription();
        DrawSettingsControls();
        DrawActionButtons();
        DrawNonStaticObjectsList();
    }

    void DrawDescription()
    {
        GUILayout.Label("This tool lists all non-static GameObjects in the scene, allowing you to view and edit their static flags individually or collectively.", EditorStyles.wordWrappedLabel);
    }

    void DrawSettingsControls()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Object Name Width:", GUILayout.Width(120));
        objectNameWidth = EditorGUILayout.IntField(objectNameWidth, GUILayout.Width(50));
        GUILayout.Label("Flag Toggle Width:", GUILayout.Width(120));
        flagToggleWidth = EditorGUILayout.IntField(flagToggleWidth, GUILayout.Width(50));
        GUILayout.EndHorizontal();
    }

    void DrawActionButtons()
    {
        if (GUILayout.Button("Show Missing Flags"))
        {
            showAllObjects = false; // Update the variable
            FindNonStaticObjects(); // Call without parameter to only include non-fully-static objects
        }
        if (GUILayout.Button("Show All Objects")) // New button to show all objects
        {
            showAllObjects = true; // Update the variable
            FindNonStaticObjects(true); // Call with includeAll set to true
        }
        if (GUILayout.Button("Sort Alphabetically"))
        {
            isSortedAlphabetically = true; // Update the variable
            SortNonStaticObjectsAlphabetically();
        }
    }

    void DrawNonStaticObjectsList()
    {
        if (nonStaticObjects != null)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (KeyValuePair<GameObject, StaticEditorFlags> pair in nonStaticObjects)
            {
                GUILayout.BeginHorizontal();
                DrawGameObjectField(pair);
                DrawFlagsToggle(pair);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }

    void DrawGameObjectField(KeyValuePair<GameObject, StaticEditorFlags> pair)
    {
        EditorGUILayout.ObjectField(pair.Key.name, pair.Key, typeof(GameObject), true, GUILayout.Width(objectNameWidth));
    }

    void DrawFlagsToggle(KeyValuePair<GameObject, StaticEditorFlags> pair)
    {
        // Master toggle for all flags
        bool allFlagsSet = AreAllFlagsSet(pair.Value);
        bool setAllFlags = GUILayout.Toggle(allFlagsSet, "All Flags", GUILayout.Width(flagToggleWidth));

        // Set or clear all flags based on the master toggle's value
        if (setAllFlags != allFlagsSet)
        {
            SetAllFlags(pair.Key, setAllFlags);
        }

        var addedFlags = new HashSet<StaticEditorFlags>();

        // Individual flag toggles
        foreach (StaticEditorFlags flag in System.Enum.GetValues(typeof(StaticEditorFlags)))
        {
            //Debug.Log(flag);
            if (addedFlags.Contains(flag))
                continue; 
            
            addedFlags.Add(flag);

            bool currentFlagSet = (pair.Value & flag) == flag;
            string flagName = flag.ToString().Replace("Static", "");
            GUIContent flagContent = new GUIContent(flagName, GetTooltipForFlag(flag));
            bool newFlagSet = GUILayout.Toggle(currentFlagSet, flagContent, GUILayout.Width(flagToggleWidth));
            if (newFlagSet != currentFlagSet)
            {
                SetStaticFlag(pair.Key, flag, newFlagSet);
            }
        }
    }

    void SortNonStaticObjectsAlphabetically()
    {
        if (nonStaticObjects != null)
        {
            // Convert the dictionary to a list of key-value pairs and sort it by GameObject name
            var sortedList = nonStaticObjects.ToList();
            sortedList.Sort((pair1, pair2) => pair1.Key.name.CompareTo(pair2.Key.name));

            // Clear the original dictionary and repopulate it with the sorted items
            nonStaticObjects.Clear();
            foreach (var pair in sortedList)
            {
                nonStaticObjects.Add(pair.Key, pair.Value);
            }
        }
    }

    void FindNonStaticObjects(bool includeAll = false)
    {
        nonStaticObjects = new Dictionary<GameObject, StaticEditorFlags>();
        foreach (GameObject obj in FindObjectsOfType<GameObject>())
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(obj);
            if (includeAll ||
                (flags & StaticEditorFlags.BatchingStatic) == 0 ||
                (flags & StaticEditorFlags.OccludeeStatic) == 0 ||
                (flags & StaticEditorFlags.OccluderStatic) == 0 ||
                (flags & StaticEditorFlags.ContributeGI) == 0 ||
                (flags & StaticEditorFlags.NavigationStatic) == 0 ||
                (flags & StaticEditorFlags.OffMeshLinkGeneration) == 0 ||
                (flags & StaticEditorFlags.ReflectionProbeStatic) == 0)
            {
                if (obj.GetComponent<MeshRenderer>())
                {
                    nonStaticObjects.Add(obj, flags);
                }
            }
        }

        if (isSortedAlphabetically) // Apply sorting if the list was previously sorted alphabetically
        {
            SortNonStaticObjectsAlphabetically();
        }
    }

    void SetStaticFlag(GameObject obj, StaticEditorFlags flag, bool set)
    {
        StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(obj);
        if (set)
        {
            currentFlags |= flag; // Set the flag
        }
        else
        {
            currentFlags &= ~flag; // Clear the flag
        }
        GameObjectUtility.SetStaticEditorFlags(obj, currentFlags);

        FindNonStaticObjects(showAllObjects); // Refresh the window to show the updated flags, respecting the current filter mode

        Repaint(); // Repaint the window to reflect the changes
    }

    string GetTooltipForFlag(StaticEditorFlags flag)
    {
        switch (flag)
        {
            case StaticEditorFlags.BatchingStatic:
                return "Enables static batching for the GameObject.";
            case StaticEditorFlags.NavigationStatic:
                return "Marks the GameObject as a navigation mesh obstacle.";
            case StaticEditorFlags.OccludeeStatic:
                return "Marks the GameObject as an occludee for occlusion culling.";
            case StaticEditorFlags.OccluderStatic:
                return "Marks the GameObject as an occluder for occlusion culling.";
            case StaticEditorFlags.OffMeshLinkGeneration:
                return "Marks the GameObject for off-mesh link generation with the Navigation system.";
            case StaticEditorFlags.ReflectionProbeStatic:
                return "Marks the GameObject to contribute to reflection probe baking.";
            case StaticEditorFlags.ContributeGI:
                return "Marks the GameObject to contribute to Global Illumination.";
            // Add more cases as needed for each StaticEditorFlags value
            default:
                return "No description available.";
        }
    }

    bool AreAllFlagsSet(StaticEditorFlags flags)
    {
        // Check if all flags are set
        return flags == (StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic |
                         StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic |
                         StaticEditorFlags.OffMeshLinkGeneration | StaticEditorFlags.ReflectionProbeStatic |
                         StaticEditorFlags.ContributeGI);
    }

    void SetAllFlags(GameObject obj, bool set)
    {
        StaticEditorFlags allFlags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic |
                                     StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic |
                                     StaticEditorFlags.OffMeshLinkGeneration | StaticEditorFlags.ReflectionProbeStatic |
                                     StaticEditorFlags.ContributeGI;

        if (set)
        {
            GameObjectUtility.SetStaticEditorFlags(obj, allFlags); // Set all flags
        }
        else
        {
            GameObjectUtility.SetStaticEditorFlags(obj, 0); // Clear all flags
        }

        FindNonStaticObjects(showAllObjects); // Refresh the window to show the updated flags, respecting the current filter mode

        if (isSortedAlphabetically) // Apply sorting if the list was previously sorted alphabetically

        {
            SortNonStaticObjectsAlphabetically();
        }

        Repaint();  // Repaint the window to reflect the changes
    }
}
