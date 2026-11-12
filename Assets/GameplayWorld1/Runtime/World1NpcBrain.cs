using UnityEngine;
using UnityEngine.AI;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(10)]
    public sealed class World1NpcBrain : MonoBehaviour
    {
        public World1Actor actor;
        public BoxCollider zone;
        public NavMeshAgent agent;
        public bool isVip;
        public int State {get;private set;}=501;
        public Vector3 Goal {get;private set;}
        float requestAt;
        bool initialized,hasPatrolGoal;

        // Native SKB_AI_Boss_Zone.IsInsideHorizontalFast4d98240 explicitly ignores Y.
        public bool Inside(Vector3 point)
        {
            var bounds=zone.bounds;return point.x>=bounds.min.x&&point.x<=bounds.max.x&&point.z>=bounds.min.z&&point.z<=bounds.max.z;
        }

        void Update()
        {
            if(actor==null||actor.session==null||!actor.session.Ready||zone==null)return;
            if(actor.IsDead){actor.SetInput(Vector3.zero,false);return;}
            if(!initialized)
            {
                initialized=true;Goal=actor.SpawnPosition;
                if(agent!=null&&NavMesh.SamplePosition(actor.transform.position,out var initial,5,NavMesh.AllAreas))
                {
                    agent.enabled=true;agent.updatePosition=false;agent.updateRotation=false;agent.autoTraverseOffMeshLink=false;agent.Warp(initial.position);
                }
            }
            var player=actor.session.player;
            bool chase=player!=null&&!player.IsDead&&Inside(player.transform.position);
            int nextState=chase?502:501; // Original Battle_PatrolInsideArea501 / Battle_Frenzy502.
            if(nextState!=State)
            {
                // The agent can keep returning its patrol route while a new path is pending.
                if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.ResetPath();
                requestAt=0;
                State=nextState;
            }
            if(chase){Goal=player.transform.position;hasPatrolGoal=false;}
            else if(!hasPatrolGoal||Vector3.ProjectOnPlane(Goal-actor.transform.position,Vector3.up).sqrMagnitude<1)
            {
                var b=zone.bounds;float pad=Mathf.Max(.5f,actor.motor.radius);
                Goal=new Vector3(Random.Range(b.min.x+pad,b.max.x-pad),actor.SpawnPosition.y,Random.Range(b.min.z+pad,b.max.z-pad));hasPatrolGoal=true;
                if(NavMesh.SamplePosition(Goal,out var sample,8,NavMesh.AllAreas))Goal=sample.position;
            }
            if(!Inside(actor.transform.position)&&Vector3.Distance(new Vector3(actor.transform.position.x,zone.bounds.center.y,actor.transform.position.z),zone.bounds.ClosestPoint(new Vector3(actor.transform.position.x,zone.bounds.center.y,actor.transform.position.z)))>5)
                actor.Revive(actor.SpawnPosition,actor.SpawnRotation);
            Vector3 steering=Goal;
            bool jump=false;
            if(agent!=null&&agent.enabled&&agent.isOnNavMesh)
            {
                agent.nextPosition=actor.transform.position;agent.speed=World1Rules.MoveSpeedAtLevel(actor.level);
                if(Time.time>=requestAt){requestAt=Time.time+.15f;agent.SetDestination(Goal);}
                if(agent.hasPath)steering=agent.steeringTarget;
                if(agent.isOnOffMeshLink){steering=agent.currentOffMeshLinkData.endPos;jump=actor.Grounded;if(Vector3.Distance(actor.transform.position,steering)<1)agent.CompleteOffMeshLink();}
            }
            Vector3 direction=steering-actor.transform.position;
            if(direction.y>.7f&&actor.Grounded)jump=true;
            actor.SetInput(Vector3.ProjectOnPlane(direction,Vector3.up).normalized,jump);
            // Original boss collision kills characters, independent of their level.
            if(chase&&Vector3.Distance(actor.transform.position,player.transform.position)<actor.motor.radius+player.motor.radius+.15f)player.Kill(3);
        }
    }
}
