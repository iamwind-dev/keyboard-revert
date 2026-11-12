using System;
using System.Collections.Generic;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    public sealed class World1Mechanic : MonoBehaviour
    {
        public World1Session session;
        public string kind;
        public int index;
        public float valueA,valueB,valueC;
        public double reward;
        public bool reverse;
        public Collider[] colliders;
        public Renderer targetRenderer;
        public Transform slot;
        public World1Mechanic target;
        public World1Mechanic[] extras;
        public float LadderTop {get{return colliders.Length>0?colliders[0].bounds.max.y:transform.position.y+valueA;}}
        public bool Fading {get;private set;}
        public bool collisionFadeEnabled=true;
        readonly HashSet<World1Actor> touching=new HashSet<World1Actor>();
        float elapsed,countdown;
        bool countdownRunning;
        MaterialPropertyBlock properties;

        void Awake()
        {
            properties=new MaterialPropertyBlock();countdown=valueA;
            // The exported lobby conveyor references two disabled colliders on an inactive object.
            // Enable the surface and sensor in the gameplay scene so contact can drive the belt.
            if(kind=="PlatformConveyor")
                foreach(var collider in colliders)
                {
                    collider.gameObject.SetActive(true);
                    collider.enabled=true;
                }
            if(kind=="PlatformFade"&&targetRenderer==null)
            {
                // The W1 keyboard has no serialized renderer. Its restored grid owns the visible surface.
                var keys=transform.Find("[reconstructed grid] keys");
                if(keys!=null)
                {
                    targetRenderer=keys.GetComponent<Renderer>();
                    var keyCollider=keys.GetComponent<MeshCollider>();
                    if(keyCollider!=null)
                    {
                        Array.Resize(ref colliders,colliders.Length+1);
                        colliders[colliders.Length-1]=keyCollider;
                    }
                }
            }
        }

        public void Enter(World1Actor actor)
        {
            if(actor==null||actor.IsDead)return;
            if(touching.Add(actor))
            {
                if(kind=="Checkpoint")session.EnterCheckpoint(actor,this);
                if(kind=="SKB_Return_Stage"&&actor.isPlayer)session.ReturnToLobby(reward);
                if(kind=="SKB_Step_Stand"&&actor.isPlayer)session.EquipStand(this);
                if(kind=="SKB_Treadmill"&&actor.isPlayer)session.EnterTreadmill(actor,this);
            }
            if(kind=="PlatformKill")actor.Kill(index);
            if(kind=="PlatformFade"&&collisionFadeEnabled)TryFade(valueA,valueB,valueC);
        }

        public void Exit(World1Actor actor)
        {
            touching.Remove(actor);
            if(kind=="Ladder")actor.ClearLadder(this);
        }

        void Update()
        {
            if(kind=="Platform_Fade_Countdown")
            {
                if(!countdownRunning){countdown=valueA;countdownRunning=true;}
                countdown-=Time.deltaTime;
                if(countdown<=0)
                {
                    if(target!=null)target.TryFade(valueB,(float)reward,valueC);
                    if(extras!=null)foreach(var platform in extras)if(platform!=null)platform.TryFade(valueB,(float)reward,valueC);
                    countdown=valueA+valueB+(float)reward+valueC;
                }
            }
            if(!Fading)return;
            elapsed+=Time.deltaTime;
            float fadeOut=Mathf.Max(.0001f,activeOut),keep=Mathf.Max(0,activeKeep),fadeIn=Mathf.Max(.0001f,activeIn);
            float alpha=elapsed<fadeOut?1-elapsed/fadeOut:elapsed<fadeOut+keep?0:Mathf.Clamp01((elapsed-fadeOut-keep)/fadeIn);
            if(targetRenderer!=null)
            {
                targetRenderer.GetPropertyBlock(properties);properties.SetFloat("_RecoveryFade",alpha);targetRenderer.SetPropertyBlock(properties);
                targetRenderer.enabled=alpha>.001f;
            }
            bool solid=elapsed<fadeOut*.9f||elapsed>=fadeOut+keep+fadeIn*.9f;
            foreach(var collider in colliders)if(collider!=null)collider.enabled=solid;
            if(elapsed>=fadeOut+keep+fadeIn)
            {
                Fading=false;foreach(var collider in colliders)if(collider!=null)collider.enabled=true;
                if(targetRenderer!=null){targetRenderer.enabled=true;properties.SetFloat("_RecoveryFade",1);targetRenderer.SetPropertyBlock(properties);}
            }
        }

        float activeOut,activeKeep,activeIn;
        // PlatformFade.TryFade native4dde030; durations come from the original component.
        public bool TryFade(float fadeOut=-1,float appearDelay=-1,float fadeIn=-1)
        {
            if(Fading||targetRenderer==null)return false;
            activeOut=fadeOut<0?valueA:fadeOut;activeKeep=appearDelay<0?valueB:appearDelay;activeIn=fadeIn<0?valueC:fadeIn;
            elapsed=0;Fading=true;return true;
        }

        void FixedUpdate()
        {
            if(kind!="PlatformConveyor")return;
            foreach(var actor in touching)if(actor!=null&&!actor.IsDead&&IsNear(actor))actor.AddConveyor(transform.forward*valueA*(reverse?-1:1));
        }
        public bool IsNear(World1Actor actor)
        {
            foreach(var collider in colliders)if(collider!=null)
            {
                var bounds=collider.bounds;bounds.Expand(actor.motor.radius*2+.5f);
                Vector3 position=actor.transform.position;
                if(kind!="Ladder"&&kind!="PlatformConveyor")position+=Vector3.up*Mathf.Min(actor.motor.height*.5f,1);
                if(bounds.Contains(position))return true;
            }
            return false;
        }
        void OnDisable(){touching.Clear();}
        void OnGUI()
        {
            if(kind!="Platform_Fade_Countdown"||Camera.main==null)return;
            Vector3 position=Camera.main.WorldToScreenPoint(transform.position);
            if(position.z>0)GUI.Label(new Rect(position.x-35,Screen.height-position.y-20,100,30),Mathf.Max(0,countdown).ToString("0.0"));
        }
    }
}
