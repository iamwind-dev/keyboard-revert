using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KeyboardEscapeGameplay.Editor
{
    [InitializeOnLoad]
    public static class World1PlaymodeChecks
    {
        const string Running="World1Checks.Running";
        static readonly List<string> checks=new List<string>();
        static readonly List<string> errors=new List<string>();
        static World1Session session;
        static World1Actor player;
        static World1NpcBrain[] brains;
        static World1Mechanic ladder,conveyor,treadmill;
        static Transform shopWindow;
        static Transform speedSlider;
        static World1ShopCatalog.Item[] auraItems;
        static Transform auraRows;
        static World1ShopPreview auraPreview;
        static int auraIndex;
        static World1Mechanic[] fadingPlatforms;
        static World1Sequence sequence;
        static Vector3 baseline;
        static float height;
        static double experience;
        static int phase,npc;
        static float began;
        static bool editorSearchIssue;

        static World1PlaymodeChecks()
        {
            EditorApplication.playModeStateChanged+=StateChanged;
            EditorApplication.update+=Update;
            SceneManager.sceneLoaded+=(_,__)=>
            {
                if(!SessionState.GetBool(Running,false))return;
                var current=ObjectFind<World1Session>();if(current!=null)current.persistProgress=false;
            };
            Application.logMessageReceived+=(message,stack,type)=>
            {
                if(!SessionState.GetBool(Running,false))return;
                if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
                if(stack.Contains("UnityEditor.Search.SearchDatabase")){editorSearchIssue=true;return;}
                errors.Add(message+" | "+stack);
            };
        }
        static T ObjectFind<T>()where T:UnityEngine.Object{return UnityEngine.Object.FindFirstObjectByType<T>();}
        static float Age {get{return Time.time-began;}}
        static void Next(int value){phase=value;began=Time.time;}
        public static void Run()
        {
            EditorSceneManager.OpenScene(World1SceneBuilder.ScenePath);
            var current=ObjectFind<World1Session>();current.persistProgress=false;
            SessionState.SetBool(Running,true);EditorApplication.EnterPlaymode();
        }
        public static void ProbeSourceNavigation()
        {
            EditorSceneManager.OpenScene(World1SceneBuilder.ScenePath);
            var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(World1SceneBuilder.SourceNavigationPath);
            if(data==null){Debug.LogError("WORLD1_SOURCE_NAV: Original NavMeshData could not be loaded.");EditorApplication.Exit(1);return;}
            var instance=NavMesh.AddNavMeshData(data);
            try
            {
                var brains=UnityEngine.Object.FindObjectsByType<World1NpcBrain>(FindObjectsSortMode.None)
                    .OrderBy(brain=>brain.transform.position.x).ToArray();
                bool covered=instance.valid&&brains.Length==4;
                foreach(var brain in brains)
                {
                    Vector3 spawn=brain.transform.position;
                    Vector3 target=spawn+brain.zone.transform.right*5;
                    bool startFound=NavMesh.SamplePosition(spawn,out var start,5,NavMesh.AllAreas);
                    bool targetFound=NavMesh.SamplePosition(target,out var end,5,NavMesh.AllAreas);
                    var path=new NavMeshPath();
                    bool found=startFound&&targetFound&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path);
                    Debug.Log($"WORLD1_SOURCE_NAV: spawn={spawn:F3}, start={startFound}/{start.position:F3}, target={targetFound}/{end.position:F3}, path={found}/{path.status}");
                    covered&=found&&path.status==NavMeshPathStatus.PathComplete;
                }
                EditorApplication.Exit(covered?0:1);
            }
            finally { if(instance.valid)instance.Remove(); }
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Running,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){checks.Clear();errors.Clear();Next(0);}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                if(phase!=99){Fail("Play mode ended before all gameplay checks completed.");return;}
                Finish(true,null);
            }
        }
        static void Check(bool value,string description)
        {
            if(!value)throw new InvalidOperationException(description);
            checks.Add(description);Debug.Log("WORLD1_GAMEPLAY_CHECK: "+description);
        }
        static void Update()
        {
            if(!SessionState.GetBool(Running,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(errors.Count>0)throw new InvalidOperationException("Runtime error: "+errors[0]);
                if(phase==0&&Age>3)
                {
                    session=ObjectFind<World1Session>();Check(session!=null&&session.Ready,"Gameplay session initialized");player=session.player;
                    Check(ObjectFind<KeyboardEscapeRecovery.MapExplorer>()==null,"Architecture explorer is absent from gameplay scene");
                    Check(session.npcs.Length==4&&session.checkpoints.Length==16,"All source NPC and checkpoint bindings loaded");
                    Check(player.motor!=null&&player.motor.enabled&&!player.IsDead,"Playable character motor is active");
                    Check(player.GetComponentsInChildren<Renderer>(true).Any(renderer=>renderer.enabled),"Original player skin is present");
                    var hud=UnityEngine.Object.FindObjectsByType<Canvas>()
                        .Single(canvas=>canvas.isActiveAndEnabled&&canvas.name=="Gameplay HUD");
                    var levelLabel=hud.transform.Find("SpeedKeyboard_GUI_View/Root/Level_Progression/Fill/Text_Level");
                    Check(levelLabel!=null&&levelLabel.GetComponent<TMPro.TextMeshProUGUI>().text=="Level 1",
                        "Source HUD shows the live level in the original progress bar");
                    Check(hud.GetComponentInChildren<UnityEngine.UI.Slider>()!=null,"HUD has the vertical speed control");
                    var catalog=session.Catalog.items;
                    Check(catalog.Length==25&&catalog.Select(item=>item.id).Distinct().Count()==25&&catalog.All(item=>item.frame&&item.icon&&item.effect),
                        "All 25 source Trail and Aura entries have their artwork and effect prefab");
                    player.GetComponent<World1PlayerInput>().enabled=false;
                    baseline=player.transform.position;experience=session.Experience;player.SetInput(Vector3.forward,false);Next(1);
                }
                else if(phase==1&&Age>1)
                {
                    Check(Vector3.ProjectOnPlane(player.transform.position-baseline,Vector3.up).magnitude>.2f,"Ground movement changes character position");
                    Check(session.Experience>experience,"Ground movement grants experience");
                    Check(session.Shoes>0&&UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>()
                        .Any(label=>label.name=="Shoes gained"&&label.text.StartsWith("+")),
                        "Movement grants shoes and shows the floating gain");
                    KeyboardEscapeRecovery.RecoveryEditor.SaveCameraImage(Camera.main,"W1_shoe_gain_test");
                    double remaining=World1Rules.GetPointToNextLevel(session.Level)-session.Experience;
                    int level=session.Level;
                    session.GrantExperience(remaining+.5);
                    Check(session.Level==level+1&&player.level==session.Level&&Math.Abs(session.Experience-.5)<.001,
                        "Earning enough shoes increases the level and carries progress forward");
                    player.SetInput(Vector3.zero,false);session.RespawnPlayer();Next(2);
                }
                else if(phase==2&&Age>1)
                {
                    height=player.transform.position.y;player.SetInput(Vector3.zero,true);Next(3);
                }
                else if(phase==3&&Age>.2)
                {
                    Check(player.transform.position.y>height+.3f,"Jump raises the character");player.SetInput(Vector3.zero,false);
                    var checkpoint=session.checkpoints[2];player.Revive(checkpoint.transform.position,checkpoint.transform.rotation);Next(4);
                }
                else if(phase==4&&Age>.7)
                {
                    Check(session.CurrentCheckpoint==2,"Checkpoint trigger updates gameplay progress");
                    Check(!player.Kill(0),"Respawn protection blocks ordinary obstacle death");
                    Check(player.Kill(101)&&player.IsDead,"Bypass-immortality hazard kills the character");Next(5);
                    Check(player.visual.GetComponentsInChildren<Rigidbody>(true).Any(body=>!body.isKinematic),"Death activates original avatar ragdoll bodies");
                }
                else if(phase==5&&Age>2.1)
                {
                    session.RespawnPlayer();Check(!player.IsDead&&player.motor.enabled,"Respawn restores the player motor");
                    Check(Vector3.Distance(player.transform.position,session.checkpoints[2].transform.position)<.2f,"Respawn uses the reached checkpoint");
                    Check(player.visual.GetComponentsInChildren<Rigidbody>(true).All(body=>body.isKinematic),"Respawn restores original avatar ragdoll to animation control");
                    brains=UnityEngine.Object.FindObjectsByType<World1NpcBrain>(FindObjectsSortMode.None)
                        .OrderBy(brain=>brain.actor.SpawnPosition.x).ToArray();
                    Check(brains.All(brain=>brain.agent.isOnNavMesh),"All four bosses bind to the World 1 NavMesh");
                    npc=0;BeginNpc();
                }
                else if(phase==6&&Age>.8)
                {
                    Check(brains[npc].State==502,"Boss "+npc+" enters chase inside its source zone");
                    var probe=brains[npc];
                    Debug.Log($"WORLD1_BOSS_PROBE {probe.name}: start={baseline:F3}, spawn={probe.actor.SpawnPosition:F3}, actor={probe.actor.transform.position:F3}, player={player.transform.position:F3}, goal={probe.Goal:F3}, velocity={probe.actor.Velocity:F3}, grounded={probe.actor.Grounded}, flags={probe.actor.motor.collisionFlags}, nav={probe.agent.enabled}/{probe.agent.isOnNavMesh}, path={probe.agent.pathStatus}, steering={probe.agent.steeringTarget:F3}");
                    Check(Vector3.ProjectOnPlane(brains[npc].actor.transform.position-baseline,Vector3.up).magnitude>.03f,"Boss "+npc+" physically moves while chasing");
                    var bounds=brains[npc].zone.bounds;player.Revive(new Vector3(bounds.max.x+20,brains[npc].actor.SpawnPosition.y,bounds.max.z+20),Quaternion.identity);Next(7);
                }
                else if(phase==7&&Age>.3)
                {
                    Check(brains[npc].State==501,"Boss "+npc+" returns to patrol after target leaves zone");
                    if(++npc<brains.Length)BeginNpc();else
                    {
                        var brain=brains[0];player.Revive(brain.actor.transform.position+Vector3.right*(brain.actor.motor.radius+player.motor.radius+.05f),Quaternion.identity);Next(8);
                    }
                }
                else if(phase==8&&Age>6)
                {
                    Check(player.IsDead&&player.LastKiller==3,"Boss contact kills player after protection expires");
                    var countdown=UnityEngine.Object.FindObjectsByType<World1Mechanic>().Single(item=>item.kind=="Platform_Fade_Countdown");
                    fadingPlatforms=new[]{countdown.target}.Concat(countdown.extras).ToArray();
                    Next(80);
                }
                else if(phase==80&&fadingPlatforms.All(platform=>!platform.Fading))Next(81);
                else if(phase==81&&(fadingPlatforms.Any(platform=>platform.Fading)||Age>8))
                {
                    Check(fadingPlatforms.All(platform=>platform.Fading),"Countdown starts fading both platform surfaces");Next(9);
                }
                else if(phase==9&&Age>1.65f)
                {
                    Check(fadingPlatforms.All(platform=>platform.colliders.All(collider=>!collider.enabled)),"Fading platform disables collision including the keyboard surface");Next(10);
                }
                else if(phase==10&&Age>2.5f)
                {
                    Check(fadingPlatforms.All(platform=>!platform.Fading&&platform.colliders.All(collider=>collider.enabled)),"Fading platform restores collision");
                    sequence=UnityEngine.Object.FindObjectsByType<World1Sequence>(FindObjectsSortMode.None).First(item=>item.definition.steps.Any(step=>step.kind=="TransformMove"&&step.value.magnitude>10));
                    baseline=sequence.transform.position;Next(11);
                }
                else if(phase==11&&Age>.7)
                {
                    Check(Vector3.Distance(sequence.transform.position,baseline)>.05f,"Source moving-platform sequence changes its original transform");
                    ladder=UnityEngine.Object.FindObjectsByType<World1Mechanic>(FindObjectsSortMode.None)
                        .Where(item=>item.kind=="Ladder"&&item.colliders[0].enabled)
                        .OrderBy(item=>(item.colliders[0].bounds.center-new Vector3(923.72f,123.63f,417.53f)).sqrMagnitude).First();
                    var bounds=ladder.colliders[0].bounds;
                    Vector3 approach=ladder.transform.forward;
                    Vector3 face=bounds.ClosestPoint(bounds.center-approach*10);
                    Vector3 start=new Vector3(face.x,bounds.min.y+.5f,face.z)-approach*(player.motor.radius+.2f);
                    player.Revive(start,Quaternion.LookRotation(approach));player.SetInput(approach,false);height=start.y;Next(12);
                }
                else if(phase==12&&Age>.5)
                {
                    Debug.Log($"WORLD1_LADDER_PROBE {ladder.name}: height={height:F3}, actor={player.transform.position:F3}, velocity={player.Velocity:F3}, active={player.ActiveLadder}, flags={player.motor.collisionFlags}, bounds={ladder.colliders[0].bounds}, up={ladder.transform.up:F3}, forward={ladder.transform.forward:F3}");
                    Check(player.transform.position.y>height+.1f,"Source ladder allows climbing");
                    height=player.transform.position.y;
                    player.SetInput(Vector3.zero,false);Next(120);
                }
                else if(phase==120&&Age>.3)
                {
                    Check(Mathf.Abs(player.transform.position.y-height)<.1f,"Player holds position when releasing ladder movement");
                    player.ClearLadder(ladder);
                    conveyor=UnityEngine.Object.FindObjectsByType<World1Mechanic>(FindObjectsSortMode.None).First(item=>item.kind=="PlatformConveyor");
                    var surface=(BoxCollider)conveyor.colliders.First(collider=>!collider.isTrigger);
                    var top=surface.transform.TransformPoint(surface.center+Vector3.up*surface.size.y*.5f);
                    player.Revive(top+Vector3.up*.1f,Quaternion.identity);baseline=player.transform.position;Next(13);
                }
                else if(phase==13&&Age>.3)
                {
                    Debug.Log($"WORLD1_CONVEYOR_PROBE: start={baseline:F3}, actor={player.transform.position:F3}, forward={conveyor.transform.forward:F3}, near={conveyor.IsNear(player)}, colliders={string.Join(", ",conveyor.colliders.Select(collider=>$"{collider.enabled}/{collider.gameObject.activeInHierarchy} {collider.bounds}"))}");
                    Check(Vector3.Dot(player.transform.position-baseline,conveyor.transform.forward)>.1f,"Conveyor transports the player");
                    session.RespawnPlayer();Next(14);
                }
                else if(phase==14&&Age>.5)
                {
                    var skinBounds=new Bounds(player.transform.position,Vector3.zero);bool first=true;
                    foreach(var renderer in player.visual.GetComponentsInChildren<Renderer>(false))if(renderer.enabled){if(first){skinBounds=renderer.bounds;first=false;}else skinBounds.Encapsulate(renderer.bounds);}
                    var detail=new VisualReport{actor=player.transform.position,motorMin=player.motor.bounds.min,skinMin=skinBounds.min,skinMax=skinBounds.max,visualLocal=player.visual.localPosition,parameters=player.visual.GetComponentsInChildren<Animator>(false).SelectMany(item=>item.parameters).Select(item=>item.name+":"+item.type).Distinct().ToArray()};
                    Renderer keyRenderer=null;
                    if(Physics.Raycast(player.transform.position+Vector3.up*3,Vector3.down,out var groundHit,10,~((1<<6)|(1<<8)|(1<<3)),QueryTriggerInteraction.Ignore))
                    {
                        detail.groundName=groundHit.collider.name;detail.groundPoint=groundHit.point;detail.groundMax=groundHit.collider.bounds.max;
                        if(groundHit.collider.name=="[reconstructed grid] keys")
                        {
                            keyRenderer=groundHit.collider.GetComponent<Renderer>();
                            detail.keyboardMax=keyRenderer.bounds.max;
                        }
                    }
                    var press=new MaterialPropertyBlock();
                    Check(keyRenderer!=null,"Player stands on a keyboard key after checkpoint respawn");
                    keyRenderer.GetPropertyBlock(press);
                    int pressId=Shader.PropertyToID("_KeyboardPress");
                    Check(press.HasVector(pressId)&&press.GetVector(pressId).x==groundHit.triangleIndex/10,"Player contact depresses the key underfoot");
                    File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/visual-diagnostics.json"),JsonUtility.ToJson(detail,true));
                    KeyboardEscapeRecovery.RecoveryEditor.SaveCameraImage(Camera.main,"W1_gameplay_test");
                    treadmill=UnityEngine.Object.FindObjectsByType<World1Mechanic>()
                        .Where(item=>item.kind=="SKB_Treadmill"&&item.index==0&&item.slot!=null)
                        .OrderBy(item=>(item.slot.position-session.checkpoints[0].transform.position).sqrMagnitude).First();
                    var sensor=treadmill.colliders[0];
                    player.Revive(sensor.bounds.center,Quaternion.identity);
                    keyRenderer.GetPropertyBlock(press);
                    Check(press.GetVector(pressId).x<0,"Leaving the key releases its press animation");
                    experience=session.Experience;Next(141);
                }
                else if(phase==141&&Age>.6f)
                {
                    Check(session.Treadmill==treadmill&&Vector3.Distance(player.transform.position,treadmill.slot.position)<.05f,
                        "Contact with a free treadmill seats the player on the source slot");
                    Check(session.Experience>experience,"Treadmill earns shoes while the player stays in place");
                    KeyboardEscapeRecovery.RecoveryEditor.SaveCameraImage(Camera.main,"W1_treadmill_test");
                    height=player.transform.position.y;player.SetInput(Vector3.zero,true);Next(142);
                }
                else if(phase==142&&Age>.16f)
                {
                    Check(session.Treadmill==null&&player.transform.position.y>height+.15f,"Jump leaves treadmill training");
                    var canvas=UnityEngine.Object.FindObjectsByType<Canvas>().Single(value=>value.name=="Gameplay HUD");
                    var slider=canvas.GetComponentInChildren<UnityEngine.UI.Slider>();
                    slider.value=0;
                    Check(Mathf.Abs(session.PlayerSpeed-World1Rules.MinimumMoveSpeed)<.001f,"Dragging speed control to the bottom applies the minimum running speed");
                    slider.value=1;
                    Check(Mathf.Abs(session.PlayerSpeed-World1Rules.MoveSpeedAtLevel(session.Level))<.001f,"Dragging speed control to the top restores the level speed");
                    session.ReturnToLobby(110);
                    var shopButton=canvas.transform.Find("SpeedKeyboard_GUI_View/Root/UI_Button_Shop");
                    Check(TopPointerHit(shopButton)==shopButton,"The Shop button receives pointer hits in the game view");
                    Click(shopButton);
                    shopWindow=canvas.transform.Find("Shop/Root");
                    Check(session.ShopOpen&&shopWindow.gameObject.activeInHierarchy,"The original HUD Shop button opens the shop");
                    speedSlider=slider.transform;
                    Next(143);
                }
                else if(phase==143&&Age>.75f)
                {
                    var modalHit=TopPointerHit(speedSlider);
                    Check(modalHit==null||!modalHit.IsChildOf(speedSlider),"The open shop blocks the speed control behind it");
                    KeyboardEscapeRecovery.RecoveryEditor.SaveCameraImage(Camera.main,"W1_shop_trails_test");
                    var root=shopWindow.Find("SafeArea/Shop_Trails/Root/Trails/ScrollRect/Viewport/Content");
                    var buy=root.Find("Trail_Green/Btn_Unlock_Entrophy");
                    Check(TopPointerHit(buy)==buy,"The Trail buy button receives pointer hits in the shop");
                    Click(buy);
                    Check(session.EquippedTrail?.id=="Trail_Green"&&session.Entropy==100&&player.GetComponentInChildren<TrailRenderer>()!=null,
                        "Buying Green Trail spends 10 cups and equips its source effect");
                    Click(root.Find("Trail_Green/Btn_Equipped"));
                    Click(root.Find("Trail_Green/Btn_Equip"));
                    Check(session.EquippedTrail?.id=="Trail_Green"&&session.Entropy==100,"An owned trail can be removed and re-equipped without another charge");
                    Click(root.Find("Trail_Purple/Btn_Get"));
                    Check(!session.Catalog.items.Where(item=>item.id=="Trail_Purple").Any(session.Owns)&&session.Entropy==100,
                        "Daily trail stays behind its original reward requirement");
                    Click(shopWindow.Find("SafeArea/ScrollRect_Tab/Viewport/Content/Tab_Aura"));
                    Next(144);
                }
                else if(phase==144&&Age>.75f)
                {
                    KeyboardEscapeRecovery.RecoveryEditor.SaveCameraImage(Camera.main,"W1_shop_auras_test");
                    var root=shopWindow.Find("SafeArea/Shop_Aura/Root/Auras/ScrollRect/Viewport/Content");
                    Click(root.Find("2.Aura_Fire/Btn_Unlock_Entrophy"));
                    Check(session.EquippedAura==null&&session.Entropy==100,"Insufficient cups cannot buy an aura");
                    Click(root.Find("1.Aura_Glow/Btn_Unlock_Entrophy"));
                    Check(session.EquippedAura?.id=="1.Aura_Glow"&&session.Entropy==0&&player.GetComponentInChildren<ParticleSystem>()!=null,
                        "Buying Glow Aura spends 100 cups and equips its source effect");
                    var auraSlot=player.transform.Find("Root/Aura_Provider/VFX_Glow_Aura");
                    Check(auraSlot!=null&&auraSlot.gameObject.activeInHierarchy&&Mathf.Approximately(auraSlot.localScale.x,.5f),
                        "Equipping Glow activates its original character slot at source scale");
                    Check(Math.Abs(session.CosmeticMultiplier-2.25)<.001,"Trail and aura stack multiplicatively for shoe earnings");
                    Click(shopWindow.Find("Title/Btn_Back"));
                    Check(!session.ShopOpen,"Shop back button returns to gameplay");
                    session.SetSpeedLimit(.25f);
                    experience=session.Shoes;
                    player.SetInput(Vector3.forward,false);
                    Next(145);
                }
                else if(phase==145&&Age>.55f)
                {
                    double gain=session.Shoes-experience;
                    Check(gain>=2.25&&Math.Abs(gain/2.25-Math.Round(gain/2.25))<.001,"Moving awards shoes at the combined equipped boost");
                    Check(Mathf.Abs(Vector3.ProjectOnPlane(player.Velocity,Vector3.up).magnitude-session.PlayerSpeed)<.1f,
                        "Player motor uses the selected speed limit during movement");
                    player.SetInput(Vector3.zero,false);
                    session.ReturnToLobby(1e12);
                    UnityEngine.Object.FindFirstObjectByType<World1ShopView>().Open();
                    Click(shopWindow.Find("SafeArea/ScrollRect_Tab/Viewport/Content/Tab_Aura"));
                    auraRows=shopWindow.Find("SafeArea/Shop_Aura/Root/Auras/ScrollRect/Viewport/Content");
                    auraPreview=UnityEngine.Object.FindFirstObjectByType<World1ShopPreview>();
                    auraItems=session.Catalog.items.Where(item=>item.aura).ToArray();
                    auraIndex=0;
                    Click(auraRows.Find(auraItems[0].id));
                    Next(146);
                }
                else if(phase==146&&Age>1.1f)
                {
                    var item=auraItems[auraIndex];
                    var skin=auraPreview.transform.Find("Preview scene/Preview character");
                    var effect=skin.Find(item.effect.name+"(Clone)");
                    float sourceOffset=auraIndex<7?.75f:0;
                    Check(effect!=null&&effect.gameObject.activeInHierarchy&&Mathf.Approximately(effect.localPosition.y,sourceOffset),
                        item.title+" preview is attached to the skin at its APK offset");
                    var particleRenderers=effect.GetComponentsInChildren<ParticleSystemRenderer>(true);
                    Check(particleRenderers.Length>0&&particleRenderers.SelectMany(renderer=>renderer.sharedMaterials)
                        .Where(material=>material!=null).All(material=>material.shader.name=="Recovery/World1Aura"),
                        item.title+" binds the recovered aura material for every particle renderer");
                    SaveAuraPreview(auraPreview.Texture,item.id);
                    var slot=player.transform.Find("Root/Aura_Provider/"+item.effect.name);
                    Check(slot!=null,item.title+" has its original World 1 slot");
                    if(!item.iap)
                    {
                        if(!session.IsEquipped(item)) Check(session.UseCosmetic(item)==null,"Equip "+item.title);
                        Check(slot.gameObject.activeInHierarchy&&slot.parent.Cast<Transform>().Count(child=>child.gameObject.activeSelf)==1,
                            item.title+" uses its original slot and replaces the prior aura");
                    }
                    if(++auraIndex<auraItems.Length)
                    {
                        Click(auraRows.Find(auraItems[auraIndex].id));
                        Next(146);
                        return;
                    }
                    Click(shopWindow.Find("Title/Btn_Back"));
                    SceneManager.LoadScene("Map1_World1_Gameplay");Next(15);
                }
                else if(phase==15&&Age>2)
                {
                    var reloaded=ObjectFind<World1Session>();
                    Check(reloaded!=null&&reloaded.Ready&&reloaded.npcs.Length==4,"Scene reload restores the gameplay session and four bosses");
                    Check(ObjectFind<KeyboardEscapeRecovery.MapExplorer>()==null,"Scene reload keeps the architecture explorer disabled");
                    Check(UnityEngine.Object.FindObjectsByType<World1Session>(FindObjectsSortMode.None).Length==1,"Scene reload does not duplicate gameplay state");
                    Next(99);EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception error){Fail(error.ToString());}
        }
        static void Click(Transform target)
        {
            if(target==null||!target.gameObject.activeInHierarchy)throw new InvalidOperationException("Expected active shop control.");
            target.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }
        static Transform TopPointerHit(Transform target)
        {
            Canvas.ForceUpdateCanvases();
            var system=UnityEngine.EventSystems.EventSystem.current;
            var pointer=new UnityEngine.EventSystems.PointerEventData(system)
            {
                position=RectTransformUtility.WorldToScreenPoint(Camera.main,target.TransformPoint(((RectTransform)target).rect.center))
            };
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();
            system.RaycastAll(pointer,hits);
            return hits.Count==0?null:hits[0].gameObject.transform;
        }
        static void SaveAuraPreview(RenderTexture target,string name)
        {
            var old=RenderTexture.active;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                image.Apply();
                var folder=Path.GetFullPath("../captures/aura-previews");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=old;
                UnityEngine.Object.Destroy(image);
            }
        }
        static void BeginNpc()
        {
            var brain=brains[npc];var bounds=brain.zone.bounds;
            brain.actor.Revive(brain.actor.SpawnPosition,brain.actor.SpawnRotation);
            Vector3 point=brain.actor.SpawnPosition+brain.zone.transform.right*5;
            point.x=Mathf.Clamp(point.x,bounds.min.x+1,bounds.max.x-1);point.z=Mathf.Clamp(point.z,bounds.min.z+1,bounds.max.z-1);
            player.Revive(point,Quaternion.identity);player.SetInput(Vector3.zero,false);baseline=brain.actor.transform.position;Next(6);
        }
        static void Fail(string message){Debug.LogError("WORLD1_PLAYMODE_FAIL: "+message);Finish(false,message);}
        static void Finish(bool passed,string error)
        {
            SessionState.SetBool(Running,false);
            var report=new Report{passed=passed,checks=checks.ToArray(),error=error,editorSearchIssue=editorSearchIssue};
            File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/playmode-checks.json"),JsonUtility.ToJson(report,true));
            Debug.Log(passed?"WORLD1_PLAYMODE_PASS":"WORLD1_PLAYMODE_FAILED");EditorApplication.Exit(passed?0:1);
        }
        [Serializable]sealed class Report{public bool passed,editorSearchIssue;public string[] checks;public string error;}
        [Serializable]sealed class VisualReport{public Vector3 actor,motorMin,skinMin,skinMax,visualLocal,groundPoint,groundMax,keyboardMax;public string groundName;public string[] parameters;}
    }
}
