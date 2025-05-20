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
        [Key(3)]public int requesterId;
        [Key(4)]public int owner;
        [Key(5)]public int netObjectId;
        [Key(6)]public string sceneId;
        public SpawnMessage(){}
        public SpawnMessage(int requesterId, string prefabName, Vector3 position, int owner = -1, string sceneId = "", List<int> target = null) : base(target)
        {
            this.requesterId = requesterId;
            this.prefabName = prefabName;
            this.position = position;
            this.owner = owner;
            this.sceneId = sceneId;
            netObjectId = -1;
        }

        public override string ToString()
        {
            return $"{base.ToString()} Prefab:{prefabName}, Pos:{position}, ID:{netObjectId}, SceneID:{sceneId}, Requester:{requesterId}, Own by :{owner}";
        }
    }
}
