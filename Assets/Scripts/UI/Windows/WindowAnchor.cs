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

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    public enum AnchorTypes
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class WindowAnchor : MonoBehaviour
    {
        [SerializeField] private AnchorTypes anchorType;

        bool attached = false;
        bool otherAttached = false;
        bool gripped = false;
        bool previouslyGripped = false;
        Transform window;
        Transform target;
        GameObject anchorObject;
        GameObject anchoredObject;

        static readonly HashSet<WindowAnchor> allAnchors = new HashSet<WindowAnchor>();

        void Start()
        {
            allAnchors.Add(this);

            anchorObject = transform.GetChild(0).gameObject;
            anchoredObject = transform.GetChild(1).gameObject;

            window = GetComponentInParent<UIHandle>().transform;
        }

        private bool IsWindowAttached()
        {
            foreach (WindowAnchor anchor in window.GetComponentsInChildren<WindowAnchor>())
            {
                if (anchor.attached)
                    return true;
            }
            return false;
        }

        private void Update()
        {
            gripped = Selection.AuxiliarySelection == window.gameObject;

            // Window hidden
            if (window.localScale == Vector3.zero)
            {
                SetTarget(null);
            }
            // Window visible
            else
            {
                if (gripped != previouslyGripped)
                {
                    ShowAllAnchors(gripped);
                }

                // Attach when release grip
                if (null != target && !gripped && !attached)
                {
                    WindowAnchor targetAnchor = target.GetComponent<WindowAnchor>();
                    if (!targetAnchor.attached && !IsWindowAttached())
                    {
                        attached = true;
                        targetAnchor.otherAttached = true;
                        anchoredObject.SetActive(true);
                    }
                }

                // Move window with attached one
                if (attached)
                {
                    if (!gripped)
                    {
                        SnapToAnchor();
                    }
                    else
                    {
                        SetTarget(null);
                    }
                }
            }

            previouslyGripped = gripped;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("WindowAnchor"))
            {
                // We may have OnTriggerEnter from the other anchor
                if (!gripped || attached) { return; }

                WindowAnchor otherAnchor = other.gameObject.GetComponent<WindowAnchor>();
                if (otherAnchor.attached)
                    return;

                AnchorTypes otherAnchorType = otherAnchor.anchorType;
                switch (anchorType)
                {
                    case AnchorTypes.Left: if (otherAnchorType != AnchorTypes.Right) { return; } break;
                    case AnchorTypes.Right: if (otherAnchorType != AnchorTypes.Left) { return; } break;
                    case AnchorTypes.Bottom: if (otherAnchorType != AnchorTypes.Top) { return; } break;
                    case AnchorTypes.Top: if (otherAnchorType != AnchorTypes.Bottom) { return; } break;
                }
                SetTarget(other.transform);
                StartCoroutine(AnimAnchors());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("WindowAnchor"))
            {
                // We may have OnTriggerEnter from the other anchor
                if (!gripped) { return; }
                SetTarget(null);
                ShowAllAnchors(true);
            }
        }

        private void SetTarget(Transform target)
        {
            if (target == this.target)
            {
                return;
            }

            if (null != this.target)
            {
                this.target.GetComponent<WindowAnchor>().otherAttached = false;
            }

            this.target = target;
            attached = false;

            anchorObject.SetActive(null == target);
            anchoredObject.SetActive(null != target);
        }

        private void SnapToAnchor()
        {
            window.rotation = target.GetComponentInParent<UIHandle>().transform.rotation;
            Vector3 offset = target.transform.position - transform.position;
            window.position += offset;
        }

        private void ShowAllAnchors(bool show)
        {
            foreach (WindowAnchor anchor in allAnchors)
            {
                if (!show)
                {
                    anchor.anchorObject.SetActive(false);
                    anchor.anchoredObject.SetActive(anchor.attached);
                }
                else
                {
                    anchor.anchorObject.SetActive(!anchor.otherAttached && !anchor.attached);
                    anchor.anchoredObject.SetActive(anchor.attached);
                }
                //anchor.anchorObject.SetActive(show && !otherAttached);
            }
        }

        private IEnumerator AnimAnchors()
        {
            float step = 0.01f;
            float factor = 1f;
            float threshold = 0.08f;
            Vector3 initScale = anchorObject.transform.localScale;
            //Vector3 targetInitScale = target.localScale;
            while (null != target && !attached)
            {
                yield return null;
                if (null == target || attached) { break; }
                float offset = factor * step;
                target.localScale += new Vector3(offset, offset, offset);
                transform.localScale += new Vector3(offset, offset, offset);
                if (Mathf.Abs(transform.localScale.x - initScale.x) >= threshold)
                {
                    factor *= -1f;
                }
            }
            //target.localScale = targetInitScale;
            anchorObject.transform.localScale = initScale;
        }
    }
}
