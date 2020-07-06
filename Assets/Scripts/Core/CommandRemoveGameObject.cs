﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandRemoveGameObject : CommandAddRemoveGameObject
    {
        ParametersController parametersController = null;
        string objectPath;

        public CommandRemoveGameObject(GameObject o) : base(o)
        {            
            parametersController = gObject.GetComponentInParent<ParametersController>();
            if (parametersController)
            {
                GameObject root = parametersController.gameObject;
                if (parametersController.GetType() == typeof(GeometryParameters) && root.transform.childCount > 0)
                {
                    objectPath = Utils.BuildTransformPath(gObject);
                }
            }
        }
        public override void Undo()
        {
            if (null == gObject) { return; }
            gObject.transform.parent.parent = parent;
            gObject.transform.parent.localPosition = position;
            gObject.transform.parent.localRotation = rotation;
            gObject.transform.parent.localScale = scale;

            Node node = SyncData.nodes[gObject.name];
            node.AddInstance(gObject);

            RestoreFromTrash(gObject, parent);
        }
        public override void Redo()
        {
            if (null == gObject) { return; }
            SendToTrash(gObject);
            gObject.transform.parent.parent = Utils.GetTrash().transform;

            Node node = SyncData.nodes[gObject.name];
            node.RemoveInstance(gObject);
        }
        public override void Submit()
        {
            position = gObject.transform.parent.localPosition;
            rotation = gObject.transform.parent.localRotation;
            scale = gObject.transform.parent.localScale;
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Serialize(SceneSerializer serializer)
        {
            if(parametersController)
            {
                Parameters parameters = parametersController.GetParameters();
                if (null != parameters)
                {
                    if (objectPath != null)
                    {
                        AssetSerializer assetSerializer = serializer.GetAssetSerializer(parameters.id);
                        assetSerializer.CreateDeletedSerializer(objectPath);
                    }
                    else
                    {
                        serializer.RemoveAsset(parameters);
                    }
                }
            }
        }

    }
}