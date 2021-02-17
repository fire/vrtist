using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace VRtist
{
    public class MixerScene : IScene
    {
        private string requestedBlenderImportName;
        private TaskCompletionSource<GameObject> blenderImportTask = null;
        private AssetBank assetBank;

        public void ClearScene()
        {
            Utils.DeleteTransformChildren(SceneManager.RightHanded);
            Utils.DeleteTransformChildren(SyncData.prefab);
            SyncData.nodes.Clear();
        }

        public GameObject InstantiateObject(GameObject prefab)
        {
            // if it does not exist in 'Prefab', create it
            if (!SyncData.nodes.TryGetValue(prefab.name, out Node prefabNode))
            {
                return SyncData.CreateFullHierarchyPrefab(prefab);
            }

            // it already exists in Prefab, duplicate it
            GameObject newPrefab = SyncData.DuplicatePrefab(prefab);
            Node newPrefabNode = SyncData.nodes[newPrefab.name];
            foreach (Node childNode in prefabNode.children)
            {
                GameObject newChildPrefab = InstantiateObject(childNode.prefab);
                Node newChildNode = SyncData.nodes[newChildPrefab.name];
                newPrefabNode.AddChild(newChildNode);
            }
            return newPrefab;
        }
        public GameObject InstantiateUnityPrefab(GameObject unityPrefab)
        {
            GameObject prefab = SyncData.CreateInstance(unityPrefab, SyncData.prefab, isPrefab: true);
            Node node = SyncData.CreateNode(prefab.name);
            node.prefab = prefab;
            return prefab;
        }

        public GameObject AddObject(GameObject gobject)
        {
            return SyncData.InstantiateFullHierarchyPrefab(gobject);
        }

        public void RemoveObject(GameObject gobject)
        {
            SendToTrashInfo trashInfo = new SendToTrashInfo
            {
                transform = gobject.transform
            };
            CommandManager.SendEvent(MessageType.SendToTrash, trashInfo);

            gobject.transform.parent.SetParent(SceneManager.Trash.transform, false);

            Node node = SyncData.nodes[gobject.name];
            node.RemoveInstance(gobject);
        }

        public void RestoreObject(GameObject gobject, Transform parent)
        {
            gobject.transform.parent.SetParent(parent, false);

            Node node = SyncData.nodes[gobject.name];
            node.AddInstance(gobject);

            RestoreFromTrashInfo trashInfo = new RestoreFromTrashInfo
            {
                transform = gobject.transform,
                parent = parent
            };
            CommandManager.SendEvent(MessageType.RestoreFromTrash, trashInfo);

        }

        public GameObject DuplicateObject(GameObject gobject)
        {
            GameObject res = SyncData.Duplicate(gobject);
            DuplicateInfos duplicateInfos = new DuplicateInfos
            {
                srcObject = gobject,
                dstObject = res
            };
            CommandManager.SendEvent(MessageType.Duplicate, duplicateInfos);

            return res;
        }

        public void RenameObject(GameObject gobject, string newName)
        {
            CommandManager.SendEvent(MessageType.Rename, new RenameInfo { srcTransform = gobject.transform, newName = gobject.name });
            SyncData.Rename(newName, gobject.name);
        }

        public void SetObjectMatrix(GameObject gobject, Matrix4x4 matrix)
        {
            string objectName = gobject.name;
            SyncData.SetTransform(gobject.name, matrix);
            foreach (var instance in SyncData.nodes[objectName].instances)
                GlobalState.FireObjectMoving(instance.Item1);
            CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
        }

        public void SetObjectTransform(GameObject gobject, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            string objectName = gobject.name;
            SyncData.SetTransform(objectName, position, rotation, scale);
            foreach (var instance in SyncData.nodes[objectName].instances)
                GlobalState.FireObjectMoving(instance.Item1);
            CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
        }
        public GameObject GetParent(GameObject gobject)
        {
            Transform parentTransform = gobject.transform.parent.parent;
            if (null == parentTransform)
                return null;
            return parentTransform.gameObject;
        }
        public void SetParent(GameObject gobject, GameObject parent)
        {
            Node parentNode = SyncData.nodes[parent.name];
            Node childNode = SyncData.nodes[gobject.name];
            parentNode.AddChild(childNode);
            gobject.transform.parent.SetParent(parent.transform, false);

            MixerClient.Instance.SendTransform(gobject.transform);
        }

        private void InformModification(GameObject gobject)
        {
            MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
            renderer.material.name = $"Mat_{gobject.name}";
            CommandManager.SendEvent(MessageType.Material, renderer.material);
            CommandManager.SendEvent(MessageType.AssignMaterial, new AssignMaterialInfo { objectName = gobject.name, materialName = renderer.material.name });
        }

        public void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Node node = SyncData.nodes[gobject.name];
            Utils.SetMaterialValue(node.prefab, materialValue);
            Utils.SetMaterialValue(gobject, materialValue);

            InformModification(gobject);
        }

        public void ListImportableObjects()
        {
            assetBank = ToolsManager.GetTool("AssetBank").GetComponent<AssetBank>();
            // Add Blender asset bank assets
            GlobalState.blenderBankImportObjectEvent.AddListener(OnBlenderBankObjectImported);
            GlobalState.blenderBankListEvent.AddListener(OnBlenderBank);
            BlenderBankInfo info = new BlenderBankInfo { action = BlenderBankAction.ListRequest };
            MixerClient.Instance.SendBlenderBank(info);
        }

        // Used for blender bank to know if a blender asset has been imported
        private void OnBlenderBankObjectImported(string objectName, string niceName)
        {
            if (niceName == requestedBlenderImportName && null != blenderImportTask && !blenderImportTask.Task.IsCompleted)
            {
                GameObject instance = SyncData.nodes[objectName].instances[0].Item1;
                blenderImportTask.TrySetResult(instance);
                Selection.AddToSelection(instance);
            }
        }

        public void OnBlenderBank(List<string> names, List<string> tags, List<string> thumbnails)
        {
            // Load only once the whole asset bank data base from Blender
            GlobalState.blenderBankListEvent.RemoveListener(OnBlenderBank);
            for (int i = 0; i < names.Count; i++)
            {
                AddBlenderAsset(names[i], tags[i], thumbnails[i]);
            }
        }
        private void AddBlenderAsset(string name, string tags, string thumbnailPath)
        {
            GameObject thumbnail = UIGrabber.CreateLazyImageThumbnail(thumbnailPath, assetBank.OnUIObjectEnter, assetBank.OnUIObjectExit);
            assetBank.AddAsset(name, thumbnail, null, tags, importFunction: ImportBlenderAsset, skipInstantiation: true);
        }
        private Task<GameObject> ImportBlenderAsset(AssetBankItem item)
        {
            requestedBlenderImportName = item.assetName;
            blenderImportTask = new TaskCompletionSource<GameObject>();
            BlenderBankInfo info = new BlenderBankInfo { action = BlenderBankAction.ImportRequest, name = item.assetName };
            MixerClient.Instance.SendBlenderBank(info);
            return blenderImportTask.Task;
        }

    }
}