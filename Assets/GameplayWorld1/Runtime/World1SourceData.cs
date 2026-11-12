using System;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [Serializable] public sealed class World1SourceData
    {
        public string sourceSceneHash;
        public ActorDefinition[] actors;
        public MechanicDefinition[] rules;
        public SequenceDefinition[] sequences;
    }
    [Serializable] public sealed class ActorDefinition
    {
        public long gameObjectId,sourceComponentId,renderTransformId,zoneColliderId;
        public bool isPlayer,isVip,fallRaycast;
        public int level;
        public float size,attackRangeMultiplier,fallThreshold,fallRayDistance;
        public string skinName,skinPrefabPath;
    }
    [Serializable] public sealed class MechanicDefinition
    {
        public string kind,sourceJson;
        public long gameObjectId,sourceComponentId,targetId;
        public long[] colliderIds,extraIds;
        public int index;
        public float a,b,c;
        public double reward;
        public bool reverse;
    }
    [Serializable] public sealed class SequenceDefinition
    {
        public long gameObjectId,sourceComponentId;
        public int loopCount,loopType,onEnable,onDisable;
        public float delay;
        public SequenceStepDefinition[] steps;
    }
    [Serializable] public sealed class SequenceStepDefinition
    {
        public string kind;
        public float duration,insertTime;
        public int addType,ease,rotateMode;
        public bool relative,changeStart,isSelf;
        public long ownerId;
        public Vector3 value,startValue;
        public CallbackDefinition[] callbacks;
    }
    [Serializable] public sealed class CallbackDefinition
    {
        public long targetId,objectId;
        public string method,stringValue;
        public int mode,intValue;
        public float floatValue;
        public bool boolValue;
    }
}
