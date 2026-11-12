using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace KeyboardEscapeGameplay.Editor
{
    public static class World1SceneBuilder
    {
        public const string ScenePath="Assets/GameplayWorld1/Scenes/Map1_World1_Gameplay.unity";
        const string Original="Assets/Recovered/_Game/Scenes/Minigame_SpeedKeyboard_W1_Optimized.unity";
        const string OriginalHash="7FEF753F2C853D2EBB0D6D3C8D8C9CE0912C084920AF7A81B390450667F1E6C9";
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}

        [MenuItem("Gameplay/World 1/Open new gameplay scene")]
        public static void Open(){EditorSceneManager.OpenScene(ScenePath);}
        public static void OpenAndPlay(){Open();EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.delayCall+=()=>EditorApplication.EnterPlaymode();}

        public static void RestoreKeyboardPhysics()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);int count=0;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))
                        if(mesh.name=="[reconstructed grid] keys"&&mesh.sharedMesh!=null)
                        {
                            var collider=mesh.GetComponent<MeshCollider>();if(!collider)collider=mesh.gameObject.AddComponent<MeshCollider>();
                            collider.sharedMesh=mesh.sharedMesh;collider.convex=false;count++;
                        }
                if(count!=197)throw new InvalidOperationException("Expected197 existing W1 grid meshes, found"+count);
                var runtime=scene.GetRootGameObjects().First(root=>root.GetComponent<World1Session>()!=null);
                BindSourceNavigation(runtime);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                if(Hash(Original)!=OriginalHash)throw new InvalidOperationException("Original scene changed during physics restoration.");
                File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/keyboard-physics.json"),"{\"restoredMeshColliders\":"+count+",\"geometryRebuilt\":false,\"sourceScenePreserved\":true}");
                Debug.Log("WORLD1_KEYBOARD_PHYSICS_PASS: "+count);EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        public static void Build()
        {
            try
            {
                if(Hash(Original)!=OriginalHash)throw new InvalidOperationException("Original W1 scene changed; refusing to use an unverified baseline.");
                var scene=EditorSceneManager.OpenScene(ScenePath);
                if(scene.GetRootGameObjects().Any(root=>root.GetComponent<World1Session>()!=null))throw new InvalidOperationException("Gameplay scene already wired; refusing to duplicate runtime components.");
                var data=JsonUtility.FromJson<World1SourceData>(File.ReadAllText("Assets/GameplayWorld1/Data/source-world1.json"));
                var objects=new Dictionary<long,Object>();
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        Add(objects,transform.gameObject);foreach(var component in transform.GetComponents<Component>())if(component!=null)Add(objects,component);
                    }
                var runtime=new GameObject("World1 gameplay runtime");runtime.AddComponent<World1GameplayScene>();
                var session=runtime.AddComponent<World1Session>();
                session.shoeIcon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Recovered/Texture2D/ic_speed.png");
                session.hudFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Recovered/Font/Nunito-Bold.ttf");
                var actors=new List<World1Actor>();
                foreach(var definition in data.actors)
                {
                    var owner=Get<GameObject>(objects,definition.gameObjectId);
                    foreach(var collider in owner.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                    foreach(var body in owner.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;}
                    var rendererRoot=Get<Transform>(objects,definition.renderTransformId);
                    foreach(Transform child in rendererRoot)child.gameObject.SetActive(false);
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(definition.skinPrefabPath);
                    if(prefab==null)throw new InvalidOperationException("Missing original skin prefab "+definition.skinPrefabPath);
                    var skin=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);skin.name="Restored skin - "+definition.skinName;skin.transform.SetParent(rendererRoot,false);skin.transform.localPosition=Vector3.zero;skin.transform.localRotation=Quaternion.identity;skin.SetActive(true);
                    rendererRoot.localScale=Vector3.one*definition.size;
                    foreach(var collider in skin.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                    foreach(var body in skin.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;}
                    foreach(var animator in skin.GetComponentsInChildren<Animator>(true))animator.enabled=true;
                    var motor=owner.AddComponent<CharacterController>();motor.radius=.3f*definition.size;motor.height=1.7f*definition.size;motor.center=Vector3.up*.85f*definition.size;motor.slopeLimit=60;motor.stepOffset=Mathf.Min(.5f*definition.size,motor.height*.4f);motor.skinWidth=.03f*definition.size;motor.minMoveDistance=0;
                    var actor=owner.AddComponent<World1Actor>();actor.session=session;actor.isPlayer=definition.isPlayer;actor.level=definition.level;actor.actorSize=definition.size;actor.isBoss=!definition.isPlayer;actor.fallThreshold=definition.fallThreshold;actor.visual=rendererRoot;actor.motor=motor;actors.Add(actor);objects[definition.sourceComponentId]=actor;
                    if(definition.isPlayer)session.player=actor;
                    else
                    {
                        var agent=owner.AddComponent<NavMeshAgent>();agent.enabled=false;agent.radius=motor.radius;agent.height=motor.height;agent.baseOffset=0;agent.stoppingDistance=.5f;agent.acceleration=1000;agent.angularSpeed=180;
                        var brain=owner.AddComponent<World1NpcBrain>();brain.actor=actor;brain.agent=agent;brain.zone=Get<BoxCollider>(objects,definition.zoneColliderId);brain.isVip=definition.isVip;
                    }
                }
                session.npcs=actors.Where(actor=>!actor.isPlayer).ToArray();
                var mechanics=new Dictionary<long,World1Mechanic>();
                foreach(var definition in data.rules)
                {
                    var owner=Get<GameObject>(objects,definition.gameObjectId);var rule=owner.AddComponent<World1Mechanic>();rule.session=session;rule.kind=definition.kind;rule.index=definition.index;rule.valueA=definition.a;rule.valueB=definition.b;rule.valueC=definition.c;rule.reward=definition.reward;rule.reverse=definition.reverse;
                    rule.colliders=definition.colliderIds.Where(id=>id!=0).Select(id=>Get<Collider>(objects,id)).ToArray();
                    if(definition.kind=="PlatformFade")rule.targetRenderer=Get<Renderer>(objects,definition.targetId);
                    if(definition.kind=="SKB_Treadmill"&&definition.targetId!=0)rule.slot=Get<Transform>(objects,definition.targetId);
                    mechanics[definition.sourceComponentId]=rule;objects[definition.sourceComponentId]=rule;
                    foreach(var collider in rule.colliders)
                    {
                        var contact=collider.GetComponent<World1Contact>();if(!contact)contact=collider.gameObject.AddComponent<World1Contact>();
                        var assigned=contact.rules==null?new List<World1Mechanic>():contact.rules.ToList();if(!assigned.Contains(rule))assigned.Add(rule);contact.rules=assigned.ToArray();
                    }
                }
                foreach(var definition in data.rules)
                    if(definition.kind=="Platform_Fade_Countdown")
                    {
                        var rule=mechanics[definition.sourceComponentId];rule.target=mechanics[definition.targetId];rule.extras=definition.extraIds.Select(id=>mechanics[id]).ToArray();rule.target.collisionFadeEnabled=false;foreach(var extra in rule.extras)extra.collisionFadeEnabled=false;
                    }
                session.checkpoints=mechanics.Values.Where(rule=>rule.kind=="Checkpoint").OrderBy(rule=>rule.index).ToArray();
                session.stepStands=mechanics.Values.Where(rule=>rule.kind=="SKB_Step_Stand").ToArray();
                foreach(var definition in data.sequences)
                {
                    var owner=Get<GameObject>(objects,definition.gameObjectId);var sequence=owner.AddComponent<World1Sequence>();sequence.definition=definition;sequence.targets=definition.steps.Select(step=>step.ownerId==0||step.isSelf?owner.transform:Get<Transform>(objects,step.ownerId)).ToArray();sequence.callbackTargets=definition.steps.SelectMany(step=>step.callbacks).Select(callback=>Get<Object>(objects,callback.targetId)).ToArray();
                }
                foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))camera.enabled=false;
                foreach(var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None))listener.enabled=false;
                foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))canvas.enabled=false;
                var cameraOwner=new GameObject("World1 player camera");var view=cameraOwner.AddComponent<Camera>();view.tag="MainCamera";view.nearClipPlane=.08f;view.farClipPlane=12000;view.fieldOfView=60;view.backgroundColor=new Color(.18f,.27f,.4f);view.clearFlags=CameraClearFlags.SolidColor;cameraOwner.AddComponent<AudioListener>();
                var follow=cameraOwner.AddComponent<World1Camera>();follow.target=session.player.transform;
                var input=session.player.gameObject.AddComponent<World1PlayerInput>();input.actor=session.player;input.view=view;
                BindSourceNavigation(runtime);
                if(session.player==null||session.npcs.Length!=4||session.checkpoints.Length!=16)throw new InvalidOperationException("Source bindings incomplete.");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                if(Hash(Original)!=OriginalHash)throw new InvalidOperationException("Original scene hash changed during gameplay build.");
                var build=EditorBuildSettings.scenes.ToList();if(!build.Any(item=>item.path==ScenePath)){build.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=build.ToArray();}
                File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/build-result.json"),"{\"built\":true,\"player\":1,\"npcs\":4,\"checkpoints\":16,\"sequences\":"+data.sequences.Length+",\"originalScenePreserved\":true}");
                Debug.Log("WORLD1_BUILD_PASS");EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }

        static void Add(Dictionary<long,Object> lookup,Object value){long id=(long)GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId;if(id!=0)lookup[id]=value;}
        static T Get<T>(Dictionary<long,Object> lookup,long id)where T:Object
        {
            if(id==0)return null;
            if(!lookup.TryGetValue(id,out Object value)||!(value is T typed))throw new InvalidOperationException("Source object ID "+id+" expected "+typeof(T).Name+", got "+value);
            return typed;
        }

        internal const string SourceNavigationPath="Assets/Recovered/NavMeshData/NavMesh-Navmesh.asset";

        static void BindSourceNavigation(GameObject runtime)
        {
            // Original W1 NavMeshSurface 144293 references this asset at identity.
            var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(SourceNavigationPath);
            if(data==null)throw new InvalidOperationException("Original W1 NavMeshData is missing.");
            var navigation=runtime.GetComponent<World1Navigation>();
            if(!navigation)navigation=runtime.AddComponent<World1Navigation>();
            navigation.data=new[]{data};
        }
    }
}
