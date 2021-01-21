﻿using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to enable/disable the DoF of a camera. In the case of enabling the DoF it may create a colimator object.
    /// </summary>
    public class CommandEnableDOF : ICommand
    {
        private static GameObject cameraColimator = null;
        readonly GameObject camera;
        readonly bool enable;

        public CommandEnableDOF(GameObject camera, bool enable)
        {
            this.camera = camera;
            this.enable = enable;
        }

        private void CreateColimator(GameObject camera)
        {
            if (null == cameraColimator)
            {
                cameraColimator = Resources.Load<GameObject>("Prefabs/UI/Colimator");
            }
            CameraController cameraController = camera.GetComponent<CameraController>();

            GameObject colimator = SyncData.CreateInstance(cameraColimator, SyncData.prefab, isPrefab: true);
            colimator.transform.localPosition = new Vector3(0, 0, -cameraController.Focus);

            ColimatorController colimatorController = colimator.GetComponent<ColimatorController>();
            colimatorController.isVRtist = true;

            Node cameraNode = SyncData.nodes[camera.name];
            Node colimatorNode = SyncData.GetOrCreateNode(colimator);
            cameraNode.AddChild(colimatorNode);
            GameObject colimatorInstance = SyncData.InstantiatePrefab(colimator);
            cameraController.colimator = colimatorInstance.transform;

            MixerClient.Instance.SendEmpty(colimator.transform);
            MixerClient.Instance.SendTransform(colimator.transform);
            MixerUtils.AddObjectToScene(colimator);
            MixerClient.Instance.SendCamera(new CameraInfo { transform = camera.transform });
        }

        private void DestroyColimator(GameObject camera)
        {
            CameraController controller = camera.GetComponent<CameraController>();
            if (null != controller.colimator)
            {
                GameObject.Destroy(controller.colimator.gameObject);
                MixerClient.Instance.SendDelete(new DeleteInfo { meshTransform = controller.colimator });
            }
        }

        private void SetDOFEnabled(bool value)
        {
            CameraController cameraController = camera.GetComponent<CameraController>();
            cameraController.EnableDOF = value;
            Transform colimator = cameraController.colimator;
            if (null == colimator)
            {
                if (value)
                {
                    CreateColimator(camera);
                }
            }
            else
            {
                if (value)
                {
                    colimator.gameObject.SetActive(true);
                    colimator.transform.localPosition = new Vector3(0, 0, -cameraController.Focus);
                    MixerClient.Instance.SendTransform(colimator.transform);
                }
                else
                {
                    ColimatorController colimatorController = colimator.GetComponent<ColimatorController>();
                    if (colimatorController.isVRtist)
                    {
                        DestroyColimator(camera);
                    }
                }
            }
        }

        public override void Undo()
        {
            SetDOFEnabled(!enable);
        }

        public override void Redo()
        {
            SetDOFEnabled(enable);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
