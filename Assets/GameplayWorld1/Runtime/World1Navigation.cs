using UnityEngine;
using UnityEngine.AI;
namespace KeyboardEscapeGameplay
{
    public sealed class World1Navigation : MonoBehaviour
    {
        public NavMeshData[] data;
        NavMeshDataInstance[] instances;
        void OnEnable(){if(data==null)return;instances=new NavMeshDataInstance[data.Length];for(int i=0;i<data.Length;i++)if(data[i]!=null)instances[i]=NavMesh.AddNavMeshData(data[i]);}
        void OnDisable(){if(instances==null)return;foreach(var instance in instances)if(instance.valid)instance.Remove();}
    }
}
