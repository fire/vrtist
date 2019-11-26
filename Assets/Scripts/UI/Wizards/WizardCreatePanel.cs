﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{

    public class WizardCreatePanel : ScriptableWizard
    {
        public UIPanel parentPanel = null;
        public string panelName = "Panel";
        public float width = 0.4f;
        public float height = 0.6f;
        public float margin = 0.02f;
        public float radius = 0.01f;
        public Material uiMaterial = null;
        public Color color = Color.white;

        private static readonly float default_width = 0.4f;
        private static readonly float default_height = 0.6f;
        private static readonly float default_margin = 0.02f;
        private static readonly float default_radius = 0.01f;

        [MenuItem("VRtist/Create UI Panel")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreatePanel>("Create UI Panel", "Create");
        }

        [MenuItem("GameObject/VRtist/UIPanel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIPanel.CreateUIPanel("Panel", parent, Vector3.zero, default_width, default_height, default_margin, default_radius, UIUtils.LoadMaterial("UIPanel"), UIElement.default_color);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIPanel";

            if (uiMaterial == null)
            {
                uiMaterial = UIUtils.LoadMaterial("UIPanel");
            }
        }

        private void OnWizardCreate()
        {
            UIPanel.CreateUIPanel(panelName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, radius, uiMaterial, color);
        }
    }
}
