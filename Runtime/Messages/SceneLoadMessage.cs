using MessagePack;

namespace NetPackage.Messages
{
    [MessagePackObject]
    public class SceneLoadMessage : NetMessage
    {
        [Key(1)] public string sceneName;
        [Key(2)] public bool isLoaded;
        [Key(3)] public int requesterId;

        public SceneLoadMessage(){}
        public SceneLoadMessage(string sceneName, int requesterId, bool isLoaded = false)
        {
            this.sceneName = sceneName;
            this.requesterId = requesterId;
            this.isLoaded = isLoaded;
        }

        public override string ToString()
        {
            return $"{base.ToString()} Scene:{sceneName}, Loaded:{isLoaded}, Requester:{requesterId}";
        }
    }
} 