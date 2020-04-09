﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers.UnityInput;
using XRTK.Definitions.Controllers.UnityInput.Profiles;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem;
using XRTK.Services;
using XRTK.Utilities;

namespace XRTK.Providers.Controllers.UnityInput
{
    /// <summary>
    /// Manages Touch devices using unity input system.
    /// </summary>
    public class UnityTouchDataProvider : BaseControllerDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="profile"></param>
        public UnityTouchDataProvider(string name, uint priority, TouchScreenControllerDataProviderProfile profile)
            : base(name, priority, profile)
        {
        }

        private static readonly Dictionary<int, UnityTouchController> ActiveTouches = new Dictionary<int, UnityTouchController>();

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            var touchCount = Input.touchCount;

            for (var i = 0; i < touchCount; i++)
            {
                var touch = Input.touches[i];

                // Construct a ray from the current touch coordinates
                var ray = CameraCache.Main.ScreenPointToRay(touch.position);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        AddTouchController(touch, ray);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        UpdateTouchData(touch, ray);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        RemoveTouchController(touch.fingerId);
                        break;
                }
            }

            foreach (var controller in ActiveTouches)
            {
                controller.Value?.Update();
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            base.Disable();

            foreach (var controller in ActiveTouches)
            {
                RemoveTouchController(controller.Key);
            }

            ActiveTouches.Clear();
        }

        private void AddTouchController(Touch touch, Ray ray)
        {
            if (!ActiveTouches.TryGetValue(touch.fingerId, out var controller))
            {
                try
                {
                    controller = new UnityTouchController(this, TrackingState.NotApplicable, Handedness.Any, GetControllerMappingProfile(typeof(UnityTouchController), Handedness.Any));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create {nameof(UnityTouchController)}!\n{e}");
                    return;
                }

                for (int i = 0; i < controller.InputSource?.Pointers.Length; i++)
                {
                    var touchPointer = (IMixedRealityTouchPointer)controller.InputSource.Pointers[i];
                    touchPointer.TouchRay = ray;
                    touchPointer.FingerId = touch.fingerId;
                }

                if (!controller.SetupConfiguration(typeof(UnityTouchController)))
                {
                    Debug.LogError($"Failed to configure {typeof(UnityTouchController).Name} controller!");
                    return;
                }

                ActiveTouches.Add(touch.fingerId, controller);
                AddController(controller);
            }

            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(controller.InputSource, controller);

            controller.StartTouch();
            UpdateTouchData(touch, ray);
        }

        private void UpdateTouchData(Touch touch, Ray ray)
        {
            if (!ActiveTouches.TryGetValue(touch.fingerId, out var controller))
            {
                return;
            }

            controller.TouchData = touch;
            var pointer = (IMixedRealityTouchPointer)controller.InputSource.Pointers[0];
            controller.ScreenPointRay = pointer.TouchRay = ray;
            controller.Update();
        }

        private void RemoveTouchController(int touchId)
        {
            if (!ActiveTouches.TryGetValue(touchId, out var controller))
            {
                return;
            }

            controller.EndTouch();
            MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            RemoveController(controller);
        }
    }
}