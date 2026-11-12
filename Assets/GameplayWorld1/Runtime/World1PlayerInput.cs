using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(10)]
    public sealed class World1PlayerInput : MonoBehaviour
    {
        public World1Actor actor;
        public Camera view;
        void Update()
        {
            if(actor==null||view==null||actor.IsDead)return;
            if(actor.session!=null&&actor.session.ShopOpen){actor.SetInput(Vector3.zero,false);return;}
            Vector3 forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
            Vector3 right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
            actor.SetInput(Vector3.ClampMagnitude(forward*Input.GetAxisRaw("Vertical")+right*Input.GetAxisRaw("Horizontal"),1),Input.GetKeyDown(KeyCode.Space));
        }
    }
}
