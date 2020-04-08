﻿//Copyright(c) XRTK.All rights reserved.
//Licensed under the MIT License.See LICENSE in the project root for license information.

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Inspectors.PropertyDrawers;

namespace XRTK.Inspectors.Profiles.InputSystem.Controllers
{
    [CustomEditor(typeof(MixedRealityControllerMappingProfile))]
    public class BaseMixedRealityControllerMappingProfileInspector : BaseMixedRealityProfileInspector
    {
        private static readonly GUIContent EditButtonContent = new GUIContent("Edit Button Mappings");

        private SerializedProperty controllerType;
        private SerializedProperty handedness;
        private SerializedProperty visualizationProfile;
        private SerializedProperty useCustomInteractions;
        private SerializedProperty interactionMappingProfiles;

        private MixedRealityControllerMappingProfile controllerMappingProfile;

        private ReorderableList interactionsList;
        private int currentlySelectedElement;

        protected override void OnEnable()
        {
            base.OnEnable();

            controllerType = serializedObject.FindProperty(nameof(controllerType));
            handedness = serializedObject.FindProperty(nameof(handedness));
            visualizationProfile = serializedObject.FindProperty(nameof(visualizationProfile));
            useCustomInteractions = serializedObject.FindProperty(nameof(useCustomInteractions));
            interactionMappingProfiles = serializedObject.FindProperty(nameof(interactionMappingProfiles));

            controllerMappingProfile = target as MixedRealityControllerMappingProfile;

            var showButtons = useCustomInteractions.boolValue;

            interactionsList = new ReorderableList(serializedObject, interactionMappingProfiles, false, false, showButtons, showButtons)
            {
                elementHeight = EditorGUIUtility.singleLineHeight * 1.5f
            };
            interactionsList.drawElementCallback += DrawConfigurationOptionElement;
            interactionsList.onAddCallback += OnConfigurationOptionAdded;
            interactionsList.onRemoveCallback += OnConfigurationOptionRemoved;
        }

        private void DrawConfigurationOptionElement(Rect position, int index, bool isActive, bool isFocused)
        {
            if (isFocused)
            {
                currentlySelectedElement = index;
            }

            position.height = EditorGUIUtility.singleLineHeight;
            position.y += 3;
            var mappingProfileProperty = interactionMappingProfiles.GetArrayElementAtIndex(index);
            MixedRealityProfilePropertyDrawer.ProfileTypeOverride = typeof(MixedRealityInteractionMappingProfile);
            EditorGUI.PropertyField(position, mappingProfileProperty, GUIContent.none);
        }

        private void OnConfigurationOptionAdded(ReorderableList list)
        {
            interactionMappingProfiles.arraySize += 1;
            var index = interactionMappingProfiles.arraySize - 1;

            var mappingProfileProperty = interactionMappingProfiles.GetArrayElementAtIndex(index);
            mappingProfileProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnConfigurationOptionRemoved(ReorderableList list)
        {
            if (currentlySelectedElement >= 0)
            {
                interactionMappingProfiles.DeleteArrayElementAtIndex(currentlySelectedElement);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            RenderHeader();

            serializedObject.Update();

            EditorGUILayout.PropertyField(controllerType);
            EditorGUILayout.PropertyField(handedness);
            EditorGUILayout.PropertyField(visualizationProfile);

            if (!useCustomInteractions.boolValue &&
                GUILayout.Button(EditButtonContent))
            {
                ControllerPopupWindow.Show(controllerMappingProfile, interactionMappingProfiles);
            }

            interactionsList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}