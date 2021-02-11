
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ProjectItem : ListItemContent
    {
        [HideInInspector] public UIDynamicListItem item;

        private float rotation = 0.0f;
        public float rotationSpeedAnglesPerSecond = 2.0f;

        public void OnDestroy()
        {
        }

        public override void SetSelected(bool value)
        {
        }

        public void SetListItem(UIDynamicListItem dlItem, string path)
        {
            item = dlItem;
            
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            Texture2D texture = Utils.LoadTexture(path, true);
            mat.SetTexture("_EquiRect", texture);
            mat.SetVector("_CamInitWorldPos", Camera.main.transform.position);

            string projectName = Directory.GetParent(path).Name;
            transform.Find("Canvas/Text").gameObject.GetComponent<TextMeshProUGUI>().text = projectName;
        }

        public void SetCameraRef(Vector3 cameraPosition)
        {
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetVector("_CamInitWorldPos", cameraPosition);
        }

        public void Rotate()
        {
            rotation += Time.unscaledDeltaTime * rotationSpeedAnglesPerSecond * Mathf.PI / 180.0f;
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetFloat("_Rotation", rotation);
        }
        public void ResetRotation(float lobbyRotation)
        {
            rotation = -lobbyRotation * Mathf.PI / 180.0f;
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetFloat("_Rotation", rotation);
        }

        public void AddListeners(UnityAction duplicateAction, UnityAction deleteAction)
        {
        }
    }
}
