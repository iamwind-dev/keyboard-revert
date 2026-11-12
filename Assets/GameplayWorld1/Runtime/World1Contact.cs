using UnityEngine;

namespace KeyboardEscapeGameplay
{
    public sealed class World1Contact : MonoBehaviour
    {
        public World1Mechanic[] rules;
        public void Touch(World1Actor actor){if(rules!=null)foreach(var rule in rules)if(rule!=null)rule.Enter(actor);}
        void OnTriggerEnter(Collider other){var actor=other.GetComponentInParent<World1Actor>();if(actor!=null)Touch(actor);}
        void OnTriggerStay(Collider other){var actor=other.GetComponentInParent<World1Actor>();if(actor!=null)Touch(actor);}
        void OnTriggerExit(Collider other){var actor=other.GetComponentInParent<World1Actor>();if(actor!=null&&rules!=null)foreach(var rule in rules)if(rule!=null)rule.Exit(actor);}
        void OnCollisionEnter(Collision collision){var actor=collision.collider.GetComponentInParent<World1Actor>();if(actor!=null)Touch(actor);}
        void OnCollisionExit(Collision collision){var actor=collision.collider.GetComponentInParent<World1Actor>();if(actor!=null&&rules!=null)foreach(var rule in rules)if(rule!=null)rule.Exit(actor);}
    }
}
