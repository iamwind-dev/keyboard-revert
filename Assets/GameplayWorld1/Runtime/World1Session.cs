using System;
using System.Globalization;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(0)]
    public sealed partial class World1Session : MonoBehaviour
    {
        public World1Actor player;
        public World1Actor[] npcs;
        public World1Mechanic[] checkpoints;
        public World1Mechanic[] stepStands;
        public Texture2D shoeIcon;
        public Font hudFont;
        public bool Ready {get;private set;}
        public int CurrentCheckpoint {get;private set;}
        public int MaxCheckpoint {get;private set;}
        public int Level {get;private set;}=1;
        public double Experience {get;private set;}
        public double Shoes {get;private set;}
        public double Entropy {get;private set;}
        public double EarnPerTick {get;private set;}=1;
        public World1Mechanic Treadmill {get;private set;}
        public string Notice => Time.time<noticeUntil?notice:null;
        public bool worldEventEnabled=true;
        public bool persistProgress=true;
        const string SaveKey="KeyboardEscape.W1.Recovered.";
        const double TestStartingCups=1_000_000_000_000d;
        const string TestCupsInitialized=SaveKey+"ShopTestCupsInitialized";
        static readonly int KeyboardPressClock=Shader.PropertyToID("_KeyboardPressClock");
        double earnClock;
        float nextEventAt,eventEnds;
        bool eventActive;
        string notice;
        float noticeUntil;
        World1Hud hud;
        World1Mechanic pendingTreadmill, departedTreadmill;
        float treadmillCooldownUntil;

        void Start()
        {
            Level=persistProgress?Mathf.Clamp(PlayerPrefs.GetInt(SaveKey+"Level",1),1,10000):1;
            double.TryParse(PlayerPrefs.GetString(SaveKey+"XP","0"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double xp);Experience=Math.Max(0,xp);
            double.TryParse(PlayerPrefs.GetString(SaveKey+"Entropy","0"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double entropy);Entropy=Math.Max(0,entropy);
            CurrentCheckpoint=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey+"Checkpoint",0),0,checkpoints.Length-1);
            MaxCheckpoint=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey+"MaxCheckpoint",0),0,checkpoints.Length-1);
            EarnPerTick=Math.Max(1,PlayerPrefs.GetFloat(SaveKey+"Earn",1));
            if(!persistProgress){Experience=0;Entropy=0;CurrentCheckpoint=0;MaxCheckpoint=0;EarnPerTick=1;}
            else if(!PlayerPrefs.HasKey(TestCupsInitialized))
            {
                Entropy=Math.Max(Entropy,TestStartingCups);
                PlayerPrefs.SetString(SaveKey+"Entropy",Entropy.ToString("R",CultureInfo.InvariantCulture));
                PlayerPrefs.SetInt(TestCupsInitialized,1);
                PlayerPrefs.Save();
            }
            if(persistProgress)
            {
                if(!double.TryParse(PlayerPrefs.GetString(SaveKey+"Shoes"),NumberStyles.Float,CultureInfo.InvariantCulture,out double shoes)
                    || double.IsNaN(shoes)||double.IsInfinity(shoes)||shoes<Experience)
                {
                    shoes=Experience;
                    for(int level=1;level<Level;level++)
                    {
                        double next=shoes+World1Rules.GetPointToNextLevel(level);
                        if(double.IsInfinity(next))break;
                        shoes=next;
                    }
                }
                Shoes=shoes;
            }
            LoadShopProgress();
            player.level=Level;
            Ready=true;
            RespawnPlayer();
            nextEventAt=Time.time+UnityEngine.Random.Range(60f,150f);
            foreach(var npc in npcs)npc.Revive(npc.transform.position,npc.transform.rotation);
            hud=gameObject.AddComponent<World1Hud>();
            hud.Initialize(this);
        }

        void Update()
        {
            if(!Ready)return;
            Shader.SetGlobalFloat(KeyboardPressClock,Time.time);
            Physics.SyncTransforms();
            if(departedTreadmill!=null&&Time.time>=treadmillCooldownUntil&&!departedTreadmill.IsNear(player))
                departedTreadmill=null;
            if(pendingTreadmill!=null)StartPendingTreadmill();
            if(!player.IsDead&&(Treadmill!=null||player.HorizontalSpeedSquared>=.25f))
            {
                earnClock+=Time.deltaTime;
                while(earnClock>=.15)
                {
                    earnClock-=.15;
                    double multiplier=eventActive?2:1;
                    double value=EarnPerTick*CosmeticMultiplier*multiplier*(Treadmill!=null?Mathf.Max(1,Treadmill.valueA)*(eventActive?8:1):1);
                    GrantExperience(value);
                }
            }
            else earnClock=0;
            if(worldEventEnabled&&!eventActive&&Time.time>=nextEventAt){eventActive=true;eventEnds=Time.time+(UnityEngine.Random.value<.7f?90:65);Notify("Sự kiện: giày ×2, máy chạy ×8");}
            if(eventActive&&Time.time>=eventEnds){eventActive=false;nextEventAt=Time.time+UnityEngine.Random.Range(60f,150f);}
            if(!ShopOpen&&player.IsDead&&player.DeadElapsed>=2&&Input.GetKeyDown(KeyCode.R))RespawnPlayer();
        }

        public void GrantExperience(double amount)
        {
            if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<=0)return;
            Shoes+=amount;
            Experience+=amount;
            int priorLevel=Level;
            while(Experience>=World1Rules.GetPointToNextLevel(Level)&&Level<10000)
            {
                Experience-=World1Rules.GetPointToNextLevel(Level);Level++;player.level=Level;
            }
            if(Level>priorLevel)
            {
                Notify("Lên level "+Level+"  •  Tốc độ tăng");
                Save();
            }
            if(hud!=null)hud.ShowGain(amount);
        }

        public void EnterCheckpoint(World1Actor actor,World1Mechanic checkpoint)
        {
            if(actor!=player||actor.IsDead)return;
            CurrentCheckpoint=checkpoint.index;
            MaxCheckpoint=Math.Max(MaxCheckpoint,CurrentCheckpoint);
            Save();
            Notify("Checkpoint "+CurrentCheckpoint+" / "+(checkpoints.Length-1));
        }

        public void RespawnPlayer()
        {
            ResetTraining();
            var target=checkpoints[Mathf.Clamp(CurrentCheckpoint,0,checkpoints.Length-1)].transform;
            player.Revive(target.position,target.rotation);
        }

        public void ReturnToLobby(double reward)
        {
            if(player.IsDead)return;
            Entropy+=Math.Max(0,reward);CurrentCheckpoint=0;RespawnPlayer();Save();Notify("Hoàn thành chặng: +"+reward.ToString("N0")+" entropy");
        }

        public void EnterTreadmill(World1Actor actor,World1Mechanic treadmill)
        {
            if(actor==player&&treadmill!=null&&Treadmill==null&&pendingTreadmill==null&&departedTreadmill!=treadmill&&treadmill.slot!=null)
                pendingTreadmill=treadmill;
        }

        void StartPendingTreadmill()
        {
            var treadmill=pendingTreadmill;
            pendingTreadmill=null;
            if(player.IsDead||!treadmill.isActiveAndEnabled)return;
            if(!treadmill.reverse&&treadmill.index!=0)
            {
                bool unlocked=treadmill.index==2&&TrySpendEntropy(treadmill,treadmill.reward);
                if(!unlocked)
                {
                    Notify(treadmill.index==2?"Cần "+treadmill.reward.ToString("N0")+" cúp để mở máy chạy."
                        :treadmill.index==3?"Máy chạy này yêu cầu gói IAP gốc.":"Máy chạy này yêu cầu mở khóa trong bản gốc.");
                    return;
                }
            }
            Treadmill=treadmill;
            earnClock=0;
            player.SnapToTreadmill(treadmill.slot);
        }

        public void LeaveTreadmill()
        {
            if(Treadmill==null)return;
            departedTreadmill=Treadmill;
            treadmillCooldownUntil=Time.time+.35f;
            Treadmill=null;
            earnClock=0;
        }

        internal void ResetTraining()
        {
            Treadmill=null;
            pendingTreadmill=null;
            departedTreadmill=null;
            earnClock=0;
        }
        public void EquipStand(World1Mechanic stand)
        {
            if(Entropy<stand.reward){Notify("Cần "+stand.reward.ToString("N0")+" entropy");return;}
            EarnPerTick=Math.Max(1,stand.valueA);Save();Notify("Tăng tốc độ luyện: +"+EarnPerTick.ToString("N0")+" / tick");
        }
        readonly System.Collections.Generic.HashSet<World1Mechanic> unlockedTreadmills=new System.Collections.Generic.HashSet<World1Mechanic>();
        public bool TrySpendEntropy(World1Mechanic owner,double price)
        {
            if(unlockedTreadmills.Contains(owner))return true;
            if(Entropy<price)return false;
            Entropy-=Math.Max(0,price);unlockedTreadmills.Add(owner);Save();return true;
        }
        public void ActorDied(World1Actor actor){if(actor==player){ResetTraining();Notify("Đã chết. Bấm R để hồi tại checkpoint sau 2 giây.");}}
        public void Notify(string value){notice=value;noticeUntil=Time.time+4;}
        void OnApplicationPause(bool paused){if(paused)Save();}
        void OnDestroy(){if(Ready)Save();CosmeticMaterials?.Dispose();}
        public void Save()
        {
            if(!persistProgress)return;
            SaveShopProgress();
            PlayerPrefs.SetString(SaveKey+"Shoes",Shoes.ToString("R",CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(SaveKey+"Level",Level);PlayerPrefs.SetString(SaveKey+"XP",Experience.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(SaveKey+"Entropy",Entropy.ToString("R",System.Globalization.CultureInfo.InvariantCulture));PlayerPrefs.SetInt(SaveKey+"Checkpoint",CurrentCheckpoint);PlayerPrefs.SetInt(SaveKey+"MaxCheckpoint",MaxCheckpoint);PlayerPrefs.SetFloat(SaveKey+"Earn",(float)EarnPerTick);PlayerPrefs.Save();
        }
    }
}
