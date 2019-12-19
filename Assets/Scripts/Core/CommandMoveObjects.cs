﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandMoveObjects : ICommand
    {
        List<GameObject> objects;

        List<Vector3> beginPositions;
        List<Quaternion> beginRotations;
        List<Vector3> beginScales;

        List<Vector3> endPositions;
        List<Quaternion> endRotations;
        List<Vector3> endScales;

        public CommandMoveObjects(List<GameObject> o, List<Vector3> bp, List<Quaternion> br, List<Vector3> bs, List<Vector3> ep, List<Quaternion> er, List<Vector3> es)
        {
            objects = o;
            beginPositions = bp;
            beginRotations = br;
            beginScales = bs;

            endPositions = ep;
            endRotations = er;
            endScales = es;
        }

        public override void Undo()
        {
            int count = objects.Count;
            for(int i = 0; i < count; i++)
            {
                if(null == objects[i]) { continue; }
                objects[i].transform.localPosition = beginPositions[i];
                objects[i].transform.localRotation = beginRotations[i];
                objects[i].transform.localScale = beginScales[i];
                CommandManager.SendEvent(MessageType.Transform, objects[i].transform);
            }
        }
        public override void Redo()
        {
            int count = objects.Count;
            for (int i = 0; i < count; i++)
            {
                if (null == objects[i]) { continue; }
                objects[i].transform.localPosition = endPositions[i];
                objects[i].transform.localRotation = endRotations[i];
                objects[i].transform.localScale = endScales[i];
                CommandManager.SendEvent(MessageType.Transform, objects[i].transform);
            }
        }
        public override void Submit()
        {
            if (objects.Count > 0)
            {
                CommandManager.AddCommand(this);
                int count = objects.Count;
                for (int i = 0; i < count; i++)
                {
                    CommandManager.SendEvent(MessageType.Transform, objects[i].transform);
                }
            }
        }
        public override void Serialize(SceneSerializer serializer)
        {
            for(int i = 0; i < objects.Count; i++)
            {
                GameObject gobject = objects[i];
                ParametersController parametersController = gobject.GetComponentInParent<ParametersController>();
                if (parametersController)
                {
                    AssetSerializer assetSerializer = serializer.GetAssetSerializer(parametersController.GetParameters().id);
                    string transformPath = Utils.BuildTransformPath(gobject);
                    TransformSerializer transformSerializer = assetSerializer.GetOrCreateTransformSerializer(transformPath);
                    transformSerializer.position = endPositions[i];
                    transformSerializer.rotation = endRotations[i];
                    transformSerializer.scale = endScales[i];
                }
            }
        }

    }
}