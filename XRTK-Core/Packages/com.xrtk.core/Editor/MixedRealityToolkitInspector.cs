﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRTK.Definitions;
using XRTK.Editor.Extensions;
using XRTK.Editor.Profiles;
using XRTK.Services;

namespace XRTK.Editor
{
    [CustomEditor(typeof(MixedRealityToolkit))]
    public class MixedRealityToolkitInspector : BaseMixedRealityToolkitInspector
    {
        private SerializedProperty activeProfile;
        private int currentPickerWindow = -1;
        private bool checkChange;

        private void Awake()
        {
            if (target.name != nameof(MixedRealityToolkit))
            {
                target.name = nameof(MixedRealityToolkit);
            }
        }

        private void OnEnable()
        {
            activeProfile = serializedObject.FindProperty(nameof(activeProfile));
            currentPickerWindow = -1;
            checkChange = activeProfile.objectReferenceValue == null;
        }

        public override void OnInspectorGUI()
        {
            RenderMixedRealityToolkitLogo();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(new GUIContent("Mixed Reality Toolkit Root Profile", "This profile is the main root configuration for the entire XRTK."));
            EditorGUILayout.PropertyField(activeProfile, GUIContent.none);
            var changed = EditorGUI.EndChangeCheck();
            var commandName = Event.current.commandName;
            var rootProfiles = ScriptableObjectExtensions.GetAllInstances<MixedRealityToolkitRootProfile>();

            if (activeProfile.objectReferenceValue == null &&
                currentPickerWindow == -1 && checkChange)
            {
                if (rootProfiles.Length > 1)
                {
                    if (rootProfiles.Length == 2)
                    {
                        var rootProfilePath = AssetDatabase.GetAssetPath(rootProfiles[1]);

                        EditorApplication.delayCall += () =>
                        {
                            changed = true;
                            var rootProfile = AssetDatabase.LoadAssetAtPath<MixedRealityToolkitRootProfile>(rootProfilePath);
                            Debug.Assert(rootProfile != null);
                            activeProfile.objectReferenceValue = rootProfile;
                            EditorGUIUtility.PingObject(rootProfile);
                            Selection.activeObject = rootProfile;
                            MixedRealityToolkit.Instance.ResetProfile(rootProfile);
                        };
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Attention!", "You must choose a profile for the Mixed Reality Toolkit.", "OK");
                        currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<MixedRealityToolkitRootProfile>(null, false, string.Empty, currentPickerWindow);
                    }
                }

                checkChange = false;
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow)
            {
                switch (commandName)
                {
                    case "ObjectSelectorUpdated":
                        activeProfile.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                        changed = true;
                        break;
                    case "ObjectSelectorClosed":
                        activeProfile.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                        currentPickerWindow = -1;
                        changed = true;
                        EditorApplication.delayCall += () =>
                        {
                            EditorGUIUtility.PingObject(activeProfile.objectReferenceValue);
                            Selection.activeObject = activeProfile.objectReferenceValue;
                        };
                        break;
                }
            }

            if (activeProfile.objectReferenceValue != null)
            {
                var rootProfile = activeProfile.objectReferenceValue as MixedRealityToolkitRootProfile;
                var profileInspector = CreateEditor(rootProfile);

                if (profileInspector is MixedRealityToolkitRootProfileInspector rootProfileInspector)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    Rect rect = new Rect(GUILayoutUtility.GetLastRect()) { height = 0.75f };
                    EditorGUI.DrawRect(rect, Color.gray);
                    EditorGUILayout.Space();

                    rootProfileInspector.RenderSystemFields();
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                EditorApplication.delayCall += () => MixedRealityToolkit.Instance.ResetProfile((MixedRealityToolkitRootProfile)activeProfile.objectReferenceValue);
            }
        }

        [MenuItem("Mixed Reality Toolkit/Configure...", true, 0)]
        private static bool CreateMixedRealityToolkitGameObjectValidation()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() == null;
        }

        [MenuItem("Mixed Reality Toolkit/Configure...", false, 0)]
        public static void CreateMixedRealityToolkitGameObject()
        {
            try
            {
                var startScene = MixedRealityPreferences.StartSceneAsset;

                if (startScene != null)
                {
                    if (!EditorUtility.DisplayDialog(
                        title: "Attention!",
                        message: $"It seems you've already set the projects Start Scene to {startScene.name}\n" +
                                 "You only need to configure the toolkit once in your Start Scene.\n" +
                                 "Would you like to replace your Start Scene with the current scene you're configuring?",
                        ok: "Yes",
                        cancel: "No"))
                    {
                        return;
                    }
                }

                Selection.activeObject = MixedRealityToolkit.Instance;
                Debug.Assert(MixedRealityToolkit.IsInitialized);

                var currentScene = SceneManager.GetActiveScene();

                if (currentScene.isDirty ||
                    string.IsNullOrWhiteSpace(currentScene.path))
                {
                    if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { currentScene }))
                    {
                        EditorApplication.delayCall += SetStartScene;
                    }
                    else
                    {
                        Debug.LogError("You must save this scene and assign it to the Start Scene in the XRTK preferences for the Mixed Reality Toolkit to function correctly.");
                    }
                }
                else
                {
                    EditorApplication.delayCall += SetStartScene;
                }

                void SetStartScene()
                {
                    var activeScene = SceneManager.GetActiveScene();
                    Debug.Assert(!string.IsNullOrEmpty(activeScene.path), "Configured Scene must be saved in order to set it as the Start Scene!\n" + "Please save your scene and set it as the Start Scene in the XRTK preferences.");
                    MixedRealityPreferences.StartSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScene.path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}