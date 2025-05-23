using System.Collections.Generic;
using MessagePack;
using UnityEngine;

namespace NetPackage.Messages
{
    [MessagePackObject]
    public class SpawnMessage : NetMessage
    {
        [Key(1)]public string prefabName;
        [Key(2)]public Vector3 position;
        [Key(3)]public Quaternion rotation;
        [Key(4)]public int requesterId;
        [Key(5)]public int owner;
        [Key(6)]public int netObjectId;
        [Key(7)]public string sceneId;
        public SpawnMessage(){}
        public SpawnMessage(int requesterId, string prefabName, Vector3 position, Quaternion rotation = default, int owner = -1, string sceneId = "", List<int> target = null) : base(target)
        {
            this.requesterId = requesterId;
            this.prefabName = prefabName;
            this.position = position;
            this.rotation = rotation;
            this.owner = owner;
            this.sceneId = sceneId;
            netObjectId = -1;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, Requester:{requesterId} Prefab:{prefabName}, ID:{netObjectId}, Own by :{owner}, Pos:{position}, Rot: {rotation} SceneID:{sceneId}";
        }
    }
}
