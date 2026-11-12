using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeyboardEscapeGameplay.Editor
{
    public static class World1GameplayChecks
    {
        public static void RunRules()
        {
            try
            {
                var type=Type.GetType("KeyboardEscapeGameplay.World1Rules, Assembly-CSharp");
                Require(type!=null,"World1Rules has not been restored from native gameplay evidence.");
                float zero=(float)type.GetMethod("DisplaySpeedToReal").Invoke(null,new object[]{0f});
                Require(Math.Abs(zero-2.9f)<.00001f,"Native display speed zero maps to real speed 2.9.");
                float hundred=(float)type.GetMethod("DisplaySpeedToReal").Invoke(null,new object[]{100f});
                Require(Math.Abs(hundred-22.9f)<.00001f,"Native display speed conversion uses (display+14.5)/5.");
                var points=type.GetMethod("GetPointToNextLevel");
                Require(Math.Abs((double)points.Invoke(null,new object[]{0})-100)<.000001,"Level zero requires source base100.");
                Require(Math.Abs((double)points.Invoke(null,new object[]{-3})-100)<.000001,"Negative level is clamped to zero in native formula.");
                double at120=(double)points.Invoke(null,new object[]{120});
                double at121=(double)points.Invoke(null,new object[]{121});
                double at420=(double)points.Invoke(null,new object[]{420});
                double at421=(double)points.Invoke(null,new object[]{421});
                Require(Math.Abs(at121/at120-(double)1.07f)<1e-9,"Source XP curve switches to1.07 at level121.");
                Require(Math.Abs(at421/at420-(double)1.12f)<1e-9,"Source XP curve switches to1.12 at level421.");
                File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/rules-checks.json"),"{\"passed\":true,\"assertions\":6}");
                Debug.Log("WORLD1_RULES_PASS: 6 assertions");
                EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogError("WORLD1_RULES_FAIL: "+error);EditorApplication.Exit(1);}
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}

        public static void RunSceneBindings()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/GameplayWorld1/Scenes/Map1_World1_Gameplay.unity");
                var type=Type.GetType("KeyboardEscapeGameplay.World1Session, Assembly-CSharp");
                Require(type!=null,"New W1 scene has no restored gameplay session/controller yet.");
                Component session=null;
                foreach(var root in scene.GetRootGameObjects()) if(root.GetComponent(type)!=null) session=root.GetComponent(type);
                Require(session!=null,"Gameplay session must be bound in the new scene.");
                var serialized=new SerializedObject(session);
                Require(serialized.FindProperty("player").objectReferenceValue!=null,"Source player character must be wired.");
                Require(serialized.FindProperty("npcs").arraySize==4,"All four source bosses must be restored.");
                Require(serialized.FindProperty("checkpoints").arraySize==16,"All sixteen source checkpoint bindings must be restored.");
                var actors=Type.GetType("KeyboardEscapeGameplay.World1Actor, Assembly-CSharp");
                int actorCount=0;
                foreach(var root in scene.GetRootGameObjects()) actorCount+=root.GetComponentsInChildren(actors,true).Length;
                Require(actorCount==5,"One player and four NPC actors must be bound to original character objects.");
                File.WriteAllText(Path.GetFullPath("../analysis/w1-gameplay/scene-bindings-checks.json"),"{\"passed\":true,\"actors\":5,\"npcs\":4,\"checkpoints\":16}");
                Debug.Log("WORLD1_BINDINGS_PASS");EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogError("WORLD1_BINDINGS_FAIL: "+error);EditorApplication.Exit(1);}
        }
    }
}
