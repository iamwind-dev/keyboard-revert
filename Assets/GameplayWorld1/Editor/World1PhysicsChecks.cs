using System;
using UnityEditor;
using UnityEngine;

namespace KeyboardEscapeGameplay.Editor
{
    public static class World1PhysicsChecks
    {
        [MenuItem("Tools/Keyboard Escape/Check physics backend")]
        public static void CheckBackend()
        {
            var backend=Physics.GetCurrentIntegrationInfo();
            if(backend.isFallback)
                throw new InvalidOperationException("Gameplay requires PhysX; active physics backend is "+backend.name);
            Debug.Log("WORLD1_PHYSICS_BACKEND_PASS: "+backend.name);
        }

        [MenuItem("Tools/Keyboard Escape/Check player motion (Play mode)")]
        public static void CheckPlayerMotion()
        {
            CheckBackend();
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
            var session=UnityEngine.Object.FindAnyObjectByType<World1Session>();
            if(session==null||!session.Ready)throw new InvalidOperationException("Gameplay session is not ready.");
            var actor=session.player;
            var position=actor.transform.position;
            var rotation=actor.transform.rotation;
            try
            {
                actor.Revive(actor.SpawnPosition+Vector3.up*3,actor.SpawnRotation);
                Require(actor.motor.bounds.size.sqrMagnitude>0,"Controller must have a native collision shape.");
                var before=actor.transform.position;
                actor.SetInput(Vector3.right,false);
                for(int i=0;i<10;i++)actor.TickMotion(1f/60);
                Require(actor.transform.position.x-before.x>.05f,"Input must move the player horizontally.");
                actor.SetInput(Vector3.zero,false);
                for(int i=0;i<120;i++)actor.TickMotion(1f/60);
                Require(actor.transform.position.y<before.y-1,"Gravity must lower the airborne player.");
                Require(actor.Grounded,"Player must land on the spawn floor.");
                float floor=actor.transform.position.y;
                actor.SetInput(Vector3.zero,true);
                actor.TickMotion(1f/60);
                Require(actor.transform.position.y>floor&&actor.Velocity.y>0,"Grounded player must jump.");
                actor.Kill(101);
                actor.Revive(actor.SpawnPosition+Vector3.up*3,actor.SpawnRotation);
                before=actor.transform.position;
                actor.SetInput(Vector3.right,false);
                actor.TickMotion(1f/60);
                Require(!actor.IsDead&&actor.motor.enabled&&actor.transform.position.x>before.x,
                    "Movement must resume after death and revival.");
                Debug.Log("WORLD1_PLAYER_MOTION_PASS: collision shape, movement, gravity, landing, jump, revive");
            }
            finally { actor.Revive(position,rotation); }
        }

        static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
        }
    }
}
