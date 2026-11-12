using System;
using System.Reflection;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    /// <summary>83 original serialized AnimationSequence tracks; native move step uses DOLocalMove.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class World1Sequence : MonoBehaviour
    {
        public SequenceDefinition definition;
        public Transform[] targets;
        public UnityEngine.Object[] callbackTargets;
        float elapsed,length;
        int priorCycle=-1;
        float priorTime=-1;
        Vector3[] from,to;
        float[] starts;
        bool initialized;

        void Start(){Initialize();}
        public void Initialize()
        {
            if(initialized||definition==null)return;
            int count=definition.steps.Length;from=new Vector3[count];to=new Vector3[count];starts=new float[count];length=0;
            for(int i=0;i<count;i++)
            {
                var step=definition.steps[i];var target=targets[i];starts[i]=length;
                length+=Mathf.Max(0,step.duration);
                if(target==null)continue;
                Vector3 initial=GetValue(target,step.kind);
                for(int previous=0;previous<i;previous++)if(targets[previous]==target&&definition.steps[previous].kind==step.kind)initial=to[previous];
                from[i]=step.changeStart?step.startValue:initial;
                to[i]=step.relative?from[i]+step.value:step.value;
            }
            initialized=true;elapsed=-definition.delay;
        }

        void Update(){Advance(Time.deltaTime);}
        public void Advance(float deltaTime)
        {
            Initialize();if(!initialized||length<=0)return;elapsed+=deltaTime;if(elapsed<0)return;
            int cycle=Mathf.FloorToInt(elapsed/length);
            if(definition.loopCount>=0&&cycle>=Mathf.Max(1,definition.loopCount))cycle=Mathf.Max(0,definition.loopCount-1);
            float local=elapsed-cycle*length;
            if(definition.loopCount>=0&&elapsed>=Mathf.Max(1,definition.loopCount)*length)local=length;
            if(definition.loopType==1&&cycle%2==1)local=length-local;
            Evaluate(local,cycle);
        }

        public void Evaluate(float localTime,int cycle=0)
        {
            Initialize();if(!initialized)return;
            int callbackIndex=0;
            for(int i=0;i<definition.steps.Length;i++)
            {
                var step=definition.steps[i];
                if(step.kind=="Callback")
                {
                    bool crossed=cycle!=priorCycle?localTime>=starts[i]:priorTime<starts[i]&&localTime>=starts[i];
                    foreach(var callback in step.callbacks)
                    {
                        if(crossed&&callbackIndex<callbackTargets.Length)Invoke(callbackTargets[callbackIndex],callback);
                        callbackIndex++;
                    }
                    continue;
                }
                if(targets[i]==null||step.kind=="Interval"||localTime<starts[i])continue;
                float progress=step.duration<=0?1:Mathf.Clamp01((localTime-starts[i])/step.duration);
                Vector3 value=Vector3.LerpUnclamped(from[i],to[i],Ease(step.ease,progress));
                if(step.kind=="TransformMove")targets[i].localPosition=value;
                else if(step.kind=="TransformRotate")
                {
                    var initial=Quaternion.Euler(from[i]);var delta=Quaternion.Euler(step.value*Ease(step.ease,progress));
                    targets[i].localRotation=step.rotateMode==2?delta*initial:step.rotateMode==3?initial*delta:step.rotateMode==0?Quaternion.Slerp(initial,Quaternion.Euler(to[i]),Ease(step.ease,progress)):Quaternion.Euler(value);
                }
                else if(step.kind=="TransformScale")targets[i].localScale=value;
            }
            priorCycle=cycle;priorTime=localTime;
        }

        static Vector3 GetValue(Transform target,string kind){return kind=="TransformMove"?target.localPosition:kind=="TransformRotate"?target.localEulerAngles:kind=="TransformScale"?target.localScale:Vector3.zero;}
        static float Ease(int ease,float t)
        {
            if(ease==4)return .5f-.5f*Mathf.Cos(Mathf.PI*t);
            if(ease==30)
            {
                const float n=7.5625f,d=2.75f;
                if(t<1/d)return n*t*t;
                if(t<2/d){t-=1.5f/d;return n*t*t+.75f;}
                if(t<2.5f/d){t-=2.25f/d;return n*t*t+.9375f;}
                t-=2.625f/d;return n*t*t+.984375f;
            }
            return t;
        }
        static void Invoke(UnityEngine.Object target,CallbackDefinition callback)
        {
            if(target==null||string.IsNullOrEmpty(callback.method))return;
            Type[] types=Type.EmptyTypes;object[] values=null;
            if(callback.mode==3){types=new[]{typeof(int)};values=new object[]{callback.intValue};}
            else if(callback.mode==4){types=new[]{typeof(float)};values=new object[]{callback.floatValue};}
            else if(callback.mode==5){types=new[]{typeof(string)};values=new object[]{callback.stringValue};}
            else if(callback.mode==6){types=new[]{typeof(bool)};values=new object[]{callback.boolValue};}
            var method=target.GetType().GetMethod(callback.method,BindingFlags.Public|BindingFlags.Instance,null,types,null);
            if(method!=null)method.Invoke(target,values);
        }
    }
}
