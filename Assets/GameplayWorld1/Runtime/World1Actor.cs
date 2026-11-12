using UnityEngine;

namespace KeyboardEscapeGameplay
{
    /// <summary>
    /// CharacterKC gameplay rules ported to a Unity CharacterController collision adapter.
    /// This adapter replaces the unavailable compiled KCC motor; it is not the original motor source.
    /// Native contracts: SetInputs4eef20c, UpdateVelocity4ef11d4, GetAppliedGravity4eed990.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class World1Actor : MonoBehaviour
    {
        public World1Session session;
        public bool isPlayer;
        public int level=1;
        public float actorSize=1,fallThreshold=20;
        public Transform visual;
        public bool isBoss;
        public CharacterController motor;
        public Vector3 Velocity {get;private set;}
        public float HorizontalSpeedSquared {get;private set;}
        public bool IsDead {get;private set;}
        public bool IsImmortal {get{return isBoss || Time.time<shieldUntil;}}
        public bool Grounded {get;private set;}
        public float DeadElapsed {get{return IsDead?Time.time-deadAt:0;}}
        public Vector3 SpawnPosition {get;private set;}
        public Quaternion SpawnRotation {get;private set;}
        public int LastKiller {get;private set;}
        public World1Mechanic ActiveLadder {get;private set;}
        Vector3 moveInput;
        Vector3 ladderHeading;
        bool jumpRequested;
        float jumpRequestedAt=float.NegativeInfinity,lastGroundAt=float.NegativeInfinity,groundY;
        float shieldUntil,deadAt;
        Transform ground;
        Vector3 groundPoint;
        Vector3 groundWorldPoint;
        Animator[] animators;
        Rigidbody[] ragdollBodies;
        Collider[] ragdollColliders;
        Vector3[] bonePositions;
        Quaternion[] boneRotations;
        World1KeyboardFeedback keyboard;

        void Awake()
        {
            motor=GetComponent<CharacterController>();
            if(isPlayer)keyboard=new World1KeyboardFeedback();
            SpawnPosition=transform.position;SpawnRotation=transform.rotation;groundY=transform.position.y;
            animators=GetComponentsInChildren<Animator>(true);
            ragdollBodies=visual==null?new Rigidbody[0]:visual.GetComponentsInChildren<Rigidbody>(false);
            ragdollColliders=new Collider[ragdollBodies.Length];bonePositions=new Vector3[ragdollBodies.Length];boneRotations=new Quaternion[ragdollBodies.Length];
            for(int i=0;i<ragdollBodies.Length;i++)
            {
                var body=ragdollBodies[i];bonePositions[i]=body.transform.localPosition;boneRotations[i]=body.transform.localRotation;ragdollColliders[i]=body.GetComponent<Collider>();
                body.isKinematic=true;if(ragdollColliders[i]!=null)ragdollColliders[i].enabled=false;
            }
            for(int i=0;i<ragdollColliders.Length;i++)for(int j=0;j<i;j++)if(ragdollColliders[i]!=null&&ragdollColliders[j]!=null)Physics.IgnoreCollision(ragdollColliders[i],ragdollColliders[j],true);
        }

        public void SetInput(Vector3 worldMove,bool jump)
        {
            moveInput=Vector3.ClampMagnitude(worldMove,1);
            if(jump){jumpRequested=true;jumpRequestedAt=Time.time;}
        }

        void Update()
        {
            if(session==null||!session.Ready||motor==null)return;
            if(IsDead)
            {
                if(!isPlayer&&DeadElapsed>=2) Revive(SpawnPosition,SpawnRotation);
                return;
            }
            Vector3 before=transform.position;
            float deltaTime=Time.deltaTime;
            TickMotion(deltaTime);
            HorizontalSpeedSquared=deltaTime>0
                ? Vector3.ProjectOnPlane(transform.position-before,Vector3.up).sqrMagnitude/(deltaTime*deltaTime)
                : 0;
            if(keyboard!=null)keyboard.Observe(transform.position,motor.skinWidth,session.Treadmill==null&&ActiveLadder==null&&Velocity.y<=0);
            UpdateAnimation();
        }

        public void TickMotion(float deltaTime)
        {
            if(deltaTime<=0||IsDead||motor==null||!motor.enabled)return;
            float speed=isPlayer&&session!=null?session.PlayerSpeed:World1Rules.MoveSpeedAtLevel(level);
            if(isPlayer&&session!=null&&session.Treadmill!=null)
            {
                if(!jumpRequested)
                {
                    Grounded=true;
                    Velocity=transform.forward*speed;
                    return;
                }
                session.LeaveTreadmill();
                lastGroundAt=Time.time;
            }
            if(ActiveLadder!=null&&!ActiveLadder.IsNear(this))ActiveLadder=null;
            if(ActiveLadder==null&&!jumpRequested)DetectLadder();
            Vector3 velocity=Velocity;
            Grounded=motor.isGrounded;
            if(Grounded)
            {
                lastGroundAt=Time.time;groundY=transform.position.y;
                if(velocity.y<0)velocity.y=-2;
                if(ground!=null)
                {
                    Vector3 next=ground.TransformPoint(groundPoint);
                    motor.Move(next-groundWorldPoint);
                    groundWorldPoint=next;
                }
            }
            ground=null;
            if(ActiveLadder!=null&&!jumpRequested)
            {
                float climbInput=Vector3.Dot(moveInput,ladderHeading);
                velocity=ActiveLadder.transform.up*(5*climbInput);
                motor.Move(velocity*deltaTime);
                Velocity=velocity;
                groundY=transform.position.y;
                if(transform.position.y>=ActiveLadder.LadderTop+motor.skinWidth)ActiveLadder=null;
                return;
            }
            Vector3 horizontal=Vector3.ProjectOnPlane(velocity,Vector3.up);
            Vector3 target=Vector3.ProjectOnPlane(moveInput,Vector3.up)*speed;
            if(Grounded) horizontal=Vector3.Lerp(horizontal,target,1-Mathf.Exp(-1000*deltaTime));
            else
            {
                horizontal=Vector3.MoveTowards(horizontal,target,1000*deltaTime);
                horizontal=Vector3.ClampMagnitude(horizontal,speed);
            }
            velocity.x=horizontal.x;velocity.z=horizontal.z;
            bool canJump=Grounded||ActiveLadder!=null||Time.time-lastGroundAt<=.5f;
            if(jumpRequested&&Time.time-jumpRequestedAt<=.5f&&canJump)
            {
                velocity.y=11;Grounded=false;lastGroundAt=float.NegativeInfinity;
                jumpRequested=false;ActiveLadder=null;ground=null;
            }
            if(Time.time-jumpRequestedAt>.5f)jumpRequested=false;
            if(!Grounded) {velocity.y-=30*deltaTime;velocity/=1+.1f*deltaTime;}
            motor.Move(velocity*deltaTime);
            if((motor.collisionFlags&CollisionFlags.Above)!=0&&velocity.y>0)velocity.y=0;
            Velocity=velocity;
            if(moveInput.sqrMagnitude>.001f)
            {
                var look=Quaternion.LookRotation(Vector3.ProjectOnPlane(moveInput,Vector3.up));
                transform.rotation=Quaternion.Slerp(transform.rotation,look,1-Mathf.Exp(-10*deltaTime));
            }
            if(groundY-transform.position.y>fallThreshold)Kill(101);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(IsDead)return;
            if(hit.normal.y>.5f)
            {
                ground=hit.collider.transform;groundPoint=ground.InverseTransformPoint(hit.point);groundWorldPoint=hit.point;
            }
            var proxy=hit.collider.GetComponent<World1Contact>();
            if(proxy!=null)proxy.Touch(this);
            var other=hit.collider.GetComponentInParent<World1Actor>();
            // SKB_Character.OnCollisionEnter4d9d460: a boss kills contacted characters (reason3).
            if(other!=null&&other!=this)
            {
                if(isBoss)other.Kill(3);
                else if(other.isBoss)Kill(3);
            }
        }

        public bool Kill(int killer)
        {
            if(IsDead||(IsImmortal&&killer!=101))return false;
            Vector3 inheritedVelocity=Velocity;
            LastKiller=killer;IsDead=true;deadAt=Time.time;Velocity=Vector3.zero;moveInput=Vector3.zero;
            HorizontalSpeedSquared=0;
            keyboard?.Release();
            motor.enabled=false;ActiveLadder=null;
            foreach(var animator in animators)if(animator)animator.enabled=false;
            for(int i=0;i<ragdollBodies.Length;i++)
            {
                if(ragdollColliders[i]!=null)ragdollColliders[i].enabled=true;
                ragdollBodies[i].isKinematic=false;ragdollBodies[i].useGravity=true;ragdollBodies[i].linearVelocity=inheritedVelocity;
            }
            session.ActorDied(this);
            return true;
        }

        public void Revive(Vector3 position,Quaternion rotation)
        {
            if(isPlayer&&session!=null)session.ResetTraining();
            keyboard?.Release();
            if(motor==null)motor=GetComponent<CharacterController>();
            for(int i=0;i<ragdollBodies.Length;i++)
            {
                var body=ragdollBodies[i];
                if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                body.isKinematic=true;if(ragdollColliders[i]!=null)ragdollColliders[i].enabled=false;
                body.transform.localPosition=bonePositions[i];body.transform.localRotation=boneRotations[i];
            }
            motor.enabled=false;transform.SetPositionAndRotation(position,rotation);motor.enabled=true;
            if(isPlayer&&session!=null)session.ClearCosmeticTrail();
            Velocity=Vector3.zero;ground=null;groundY=position.y;moveInput=Vector3.zero;ActiveLadder=null;
            HorizontalSpeedSquared=0;
            jumpRequested=false;lastGroundAt=float.NegativeInfinity;IsDead=false;shieldUntil=Time.time+5;
            foreach(var animator in animators)if(animator)animator.enabled=true;
        }

        internal void SnapToTreadmill(Transform slot)
        {
            keyboard?.Release();
            ground=null;
            ActiveLadder=null;
            jumpRequested=false;
            motor.enabled=false;
            transform.SetPositionAndRotation(slot.position,slot.rotation);
            session.ClearCosmeticTrail();
            motor.enabled=true;
            groundY=slot.position.y;
            Velocity=Vector3.zero;
            HorizontalSpeedSquared=0;
        }

        // CharacterKC.DetectLadder (0x4ef0d30) checks the ladder layer ahead and uses the hit normal.
        void DetectLadder()
        {
            Vector3 direction=Vector3.ProjectOnPlane(moveInput,Vector3.up);
            if(direction.sqrMagnitude<.01f)return;
            direction.Normalize();
            if(!Physics.BoxCast(transform.position+Vector3.up*.35f,Vector3.one*.1f,direction,
                out var hit,Quaternion.LookRotation(direction),motor.radius+.1f,1<<9,QueryTriggerInteraction.Ignore))return;
            var contact=hit.collider.GetComponent<World1Contact>();
            if(contact==null||contact.rules==null)return;
            Vector3 heading=Vector3.ProjectOnPlane(-hit.normal,Vector3.up).normalized;
            if(Vector3.Dot(direction,heading)<=0)return;
            foreach(var rule in contact.rules)
                if(rule!=null&&rule.isActiveAndEnabled&&rule.kind=="Ladder")
                {
                    ActiveLadder=rule;
                    ladderHeading=heading;
                    return;
                }
        }

        public void ClearLadder(World1Mechanic ladder){if(ActiveLadder==ladder)ActiveLadder=null;}
        public void AddConveyor(Vector3 velocity){if(!IsDead&&motor.enabled)motor.Move(velocity*Time.fixedDeltaTime);}

        void UpdateAnimation()
        {
            float normalized=Vector3.ProjectOnPlane(Velocity,Vector3.up).magnitude/Mathf.Max(1,World1Rules.MoveSpeedAtLevel(level));
            foreach(var animator in animators)
            {
                if(!animator||!animator.isActiveAndEnabled||animator.runtimeAnimatorController==null)continue;
                foreach(var parameter in animator.parameters)
                {
                    string name=parameter.name.ToLowerInvariant();
                    if(parameter.type==AnimatorControllerParameterType.Float&&(name.Contains("speed")||name.Contains("velocity")||name.Contains("move")))animator.SetFloat(parameter.nameHash,normalized);
                    if(parameter.type==AnimatorControllerParameterType.Bool&&name.Contains("ground"))animator.SetBool(parameter.nameHash,Grounded);
                    if(parameter.type==AnimatorControllerParameterType.Bool&&(name.Contains("walk")||name.Contains("run")||name.Contains("move")))animator.SetBool(parameter.nameHash,normalized>.05f);
                    if(parameter.type==AnimatorControllerParameterType.Bool&&name.Contains("jump"))animator.SetBool(parameter.nameHash,!Grounded);
                }
            }
        }
    }
}
