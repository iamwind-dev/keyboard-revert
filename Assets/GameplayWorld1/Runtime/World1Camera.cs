using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(100)]
    public sealed class World1Camera : MonoBehaviour
    {
        public Transform target;
        public float distance=6,yaw,pitch=18;
        World1Actor actor;
        World1Mechanic trainingOn;
        float trainingYaw,trainingPitch;

        void Awake(){if(target!=null)actor=target.GetComponent<World1Actor>();}

        void LateUpdate()
        {
            if(target==null)return;
            if(actor!=null&&actor.session!=null&&actor.session.ShopOpen)
            {
                Cursor.lockState=CursorLockMode.None;
                Cursor.visible=true;
                return;
            }
            var treadmill=actor!=null&&actor.session!=null?actor.session.Treadmill:null;
            if(treadmill!=trainingOn)
            {
                trainingOn=treadmill;
                if(trainingOn!=null)
                {
                    trainingYaw=target.eulerAngles.y+130;
                    trainingPitch=42;
                }
            }
            bool looking=Input.GetMouseButton(1);
            Cursor.lockState=looking?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!looking;
            if(looking)
            {
                float horizontal=Input.GetAxisRaw("Mouse X")*6;
                float vertical=Input.GetAxisRaw("Mouse Y")*6;
                if(trainingOn!=null)
                {
                    trainingYaw+=horizontal;
                    trainingPitch=Mathf.Clamp(trainingPitch-vertical,5,75);
                }
                else
                {
                    yaw+=horizontal;
                    pitch=Mathf.Clamp(pitch-vertical,-45,75);
                }
            }
            distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.5f,2,9);
            Vector3 pivot=target.position+Vector3.up;
            Quaternion rotation=trainingOn!=null
                ? Quaternion.Euler(trainingPitch,trainingYaw,0)
                : Quaternion.Euler(pitch,yaw,0);
            Vector3 direction=rotation*Vector3.back;
            float actual=distance;
            if(Physics.SphereCast(pivot,.2f,direction,out var hit,distance,~((1<<6)|(1<<8)|(1<<3)),QueryTriggerInteraction.Ignore))actual=Mathf.Max(.5f,hit.distance-.2f);
            transform.SetPositionAndRotation(pivot+direction*actual,rotation);
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
