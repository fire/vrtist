﻿/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class NavigationMode_Fly : NavigationMode
    {
        private float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        private float minPlayerScale = 50.0f; // world scale = 50.0f;

        private float flySpeed = 0.2f;
        private bool rotating = false;

        private Matrix4x4 initLeftControllerMatrix_WtoL;
        private Matrix4x4 initRigMatrix_W;

        private float scale;
        private bool isLeftGripped = false;

        private const float deadZone = 0.3f;
        private const float fixedScaleFactor = 1.05f; // for grip world scale

        Matrix4x4 initPivotMatrix;

        public NavigationMode_Fly(float speed, float minScale, float maxScale)
        {
            flySpeed = speed;
            minPlayerScale = minScale;
            maxPlayerScale = maxScale;
        }

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform rightHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, rightHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            // Create tooltips
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Joystick, Tooltips.Action.Joystick, "Move Turn");
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Grip, Tooltips.Action.HoldPush, "Grip World");

            usedControls = UsedControls.LEFT_GRIP | UsedControls.LEFT_JOYSTICK;
        }

        public override void Update()
        {
            // TODO: on garde le rotate 45 degres ou on le reserve au mode teleport (et on fait du continu vomitif pour le mode fly)?

            //
            // Joystick -- go forward/backward, and rotate 45 degrees.
            //

            Vector2 val = VRInput.GetValue(VRInput.secondaryController, CommonUsages.primary2DAxis);
            if (val != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                Vector3 velocity = Camera.main.transform.forward * val.y * d;
                rig.position += velocity * flySpeed;

                if (Mathf.Abs(val.x) > 0.95f && !rotating)
                {
                    rig.rotation *= Quaternion.Euler(0f, Mathf.Sign(val.x) * 45f, 0f);
                    rotating = true;
                }
                if (Mathf.Abs(val.x) <= 0.95f && rotating)
                {
                    rotating = false;
                }
            }

            //
            // LEFT GRIP WORLD (on click)
            //

            VRInput.ButtonEvent(VRInput.secondaryController, CommonUsages.gripButton,
            () =>
            {
                ResetInitControllerMatrices();
                ResetInitWorldMatrix();

                SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
                isLeftGripped = true;
                GlobalState.IsGrippingWorld = true;
            },
            () =>
            {
                SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
                isLeftGripped = false;
                GlobalState.IsGrippingWorld = false;
            });

            // NOTE: we test isLeftGrip because we can be ungripped but still over the deadzone, strangely.
            if (isLeftGripped && VRInput.GetValue(VRInput.secondaryController, CommonUsages.grip) > deadZone)
            {
                float prevScale = scale;

                // Scale using left joystick.
                Vector2 joystickAxis = VRInput.GetValue(VRInput.secondaryController, CommonUsages.primary2DAxis);
                if (joystickAxis.y > deadZone)
                    scale *= fixedScaleFactor;
                if (joystickAxis.y < -deadZone)
                    scale /= fixedScaleFactor;

                // update left joystick
                Vector3 currentLeftControllerPosition_L;
                Quaternion currentLeftControllerRotation_L;
                VRInput.GetControllerTransform(VRInput.secondaryController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
                Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));

                Matrix4x4 currentLeftControllerMatrix_W_Delta = initPivotMatrix * currentLeftControllerMatrix_L_Scaled * initLeftControllerMatrix_WtoL;
                Matrix4x4 transformed = currentLeftControllerMatrix_W_Delta * initRigMatrix_W;

                transformed = transformed.inverse;

                rig.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                rig.localRotation = transformed.rotation;
                float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / maxPlayerScale, minPlayerScale);
                rig.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
                if (transformed.lossyScale.x != clampedScale)
                {
                    scale = prevScale;
                }

                GlobalState.WorldScale = 1f / rig.localScale.x;

                UpdateCameraClipPlanes();
            }
        }

        private void ResetInitControllerMatrices()
        {
            Vector3 initLeftControllerPosition_L;
            Quaternion initLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.secondaryController, out initLeftControllerPosition_L, out initLeftControllerRotation_L);
            Matrix4x4 initLeftControllerMatrix_L = Matrix4x4.TRS(initLeftControllerPosition_L, initLeftControllerRotation_L, Vector3.one);
            initPivotMatrix = Matrix4x4.TRS(pivot.localPosition, pivot.localRotation, pivot.localScale);
            initLeftControllerMatrix_WtoL = (initPivotMatrix * initLeftControllerMatrix_L).inverse;
        }

        private void ResetInitWorldMatrix()
        {
            initRigMatrix_W = rig.worldToLocalMatrix;
            scale = 1f;
            GlobalState.WorldScale = scale;
        }
    }
}
