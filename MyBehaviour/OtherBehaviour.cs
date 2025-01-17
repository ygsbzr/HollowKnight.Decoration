﻿using DecorationMaster.Attr;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using DecorationMaster.Util;
using TMPro;
using DecorationMaster.UI;
using System;
using System.Collections;
using Modding;
using System.Linq;

namespace DecorationMaster.MyBehaviour
{
    public class OtherBehaviour
    {
        [Description("电锯，是这个MOD最初的装饰品，也是MOD名字——装修大师 的由来")]
        [Description("saw, if you like it ,you can decorate it for the whole Hallownest", "en-us")]
        [Decoration("HK_saw")]
        public class Saw : SawMovement
        {
            public override Vector3 Move(Vector3 current)
            {
                var sitem = item as ItemDef.SawItem;

                float dealtDis = sitem.span * Mathf.Sin(sitem.speed * Time.time + sitem.offset * Mathf.PI / 2);
                float dx = dealtDis * Mathf.Cos(sitem.angle * Mathf.PI / 180f);
                float dy = dealtDis * Mathf.Sin(sitem.angle * Mathf.PI / 180f);
                return new Vector3(sitem.Center.x + dx, sitem.Center.y + dy, current.z);
            }
            public override void Hit(HitInstance damageInstance)
            {
                if (damageInstance.AttackType == AttackTypes.Nail)
                    base.Hit(damageInstance);
                else
                {
                    //float pitch = gameObject.GetComponent<MyTinkEffect>().pitch;
                }
            }
            [Handle(Operation.SetVolume)]
            public void HandleVoice(float val)
            {
                gameObject.GetComponent<AudioSource>().volume = val;
            }
            public override void HandlePos(Vector2 val)
            {
                return;
            }
        }

        [Description("放置时可能会有些bug:到处飘，这个只需要重新进入场景就不会出现了")]
        [Description("maybe some issue while placing, but when you re-enter scene it will work", "en-us")]
        [Decoration("HK_fly")]
        public class Fly : Resizeable
        {
            public void OnTriggerEnter2D(Collider2D col)
            {
                if (col.gameObject.layer == (int)GlobalEnums.PhysLayers.HERO_ATTACK && col.name.Contains("Slash"))
                {
                    if (SetupMode)
                        Remove();
                }
            }

        }

        [Description("碑文")]
        [Decoration("HK_lore_tablet_1")]
        public class LoreTablet1 : Resizeable
        {
            [Handle(Operation.SetText)]
            public void UpdateDialogue(string text)
            {
                if (string.IsNullOrEmpty(text))
                    return;
                string val = text;
                string key = $"LoreTablet1_{val.GetHashCode()}";
                if (DLanguage.MyLan.ContainsKey(key))
                    DLanguage.MyLan[key] = val;
                else
                    DLanguage.MyLan.Add(key, val);

                //inspect.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmString("Game Text Convo").Value = key;
                gameObject.LocateMyFSM("Inspection").FsmVariables.GetFsmString("Convo Name").Value = key;
            }
        }

        [Description("由于放置时它把自己炸死会出BUG，所以我把血量设为了9999")]
        [Description("it has 9999 HP lol", "en-us")]
        [Decoration("HK_turret")]
        public class Turret : Resizeable
        {
            private void Awake()
            {
                var hm = gameObject.GetComponent<HealthManager>();
                if (hm)
                    hm.hp = 9999;
            }
            public void OnTriggerEnter2D(Collider2D col)
            {
                if (col.gameObject.layer == (int)GlobalEnums.PhysLayers.HERO_ATTACK && col.name.Contains("Slash"))
                {
                    if (SetupMode)
                        Remove();
                }
            }

        }
        [Description("开关，注意和门对应编号。\n当然，你要多个开关对应一个门我也不拦着你")]
        [Description("switch just like you see. \n be care that you mush match switch number to gate number \n of course, you can match not just one switch for a gate", "en-us")]
        [Decoration("HK_lever")]
        public class Lever : Resizeable
        {
            private GameObject numDisp;
            private PlayMakerFSM playMakerFSM;
            public void Awake()
            {
                playMakerFSM = gameObject.LocateMyFSM("toll switch");
                playMakerFSM.SetState("Pause");
            }
            public void Start()
            {
                int gateNum = ((ItemDef.LeverGateItem)item).Number;
                var gateName = $"{ItemDef.LeverGateItem.GateNamePrefix}{gateNum}";
                playMakerFSM.GetAction<FindGameObject>("Initiate", 2).objectName = gateName;

                if (ItemManager.Instance.setupMode)
                {
                    numDisp = NameDisp.Create(gameObject, $"{gateNum}");

                    if (gateNum == 0)
                    {
                        var exists = FindObjectsOfType<Lever>().Length;
                        Setup(Operation.SetGate, exists);
                    }
                }
            }
            [Handle(Operation.SetGate)]
            public void HandleGateNumber(int num)
            {
                if (numDisp != null)
                {
                    numDisp.GetComponent<TextMeshPro>().text = num.ToString();
                }
                int gateNum = ((ItemDef.LeverGateItem)item).Number;
                var gateName = $"{ItemDef.LeverGateItem.GateNamePrefix}{gateNum}";
                playMakerFSM = gameObject.LocateMyFSM("toll switch");
                playMakerFSM.GetAction<FindGameObject>("Initiate", 2).objectName = gateName;
            }
        }

        [Description("由拉杆开关触发的门，注意编号对应。\n一个门可以有多个开关，但是一个开关只能开一个门")]
        [Description("gate \n be care that you must match gate number to switch number. \n one switch can just open one gate.", "en-us")]
        [Decoration("HK_gate")]
        public class Gate : Resizeable
        {
            private GameObject numDisp;
            public void Start()
            {
                int gateNum = ((ItemDef.LeverGateItem)item).Number;
                var gateName = $"{ItemDef.LeverGateItem.GateNamePrefix}{gateNum}";
                gameObject.name = gateName;

                if (ItemManager.Instance.setupMode)
                {
                    numDisp = NameDisp.Create(gameObject, $"{gateNum}");

                    if (gateNum == 0)
                    {
                        var exists = FindObjectsOfType<Gate>().Length;
                        Setup(Operation.SetGate, exists);
                    }
                }
            }

            [Handle(Operation.SetGate)]
            public void HandleGateNumber(int num)
            {
                if (numDisp != null)
                {
                    numDisp.GetComponent<TextMeshPro>().text = num.ToString();
                }
                int gateNum = ((ItemDef.LeverGateItem)item).Number;
                var gateName = $"{ItemDef.LeverGateItem.GateNamePrefix}{gateNum}";
                gameObject.name = gateName;
            }
        }

        [Description("危险重生点，你可以理解为存档点")]
        [Description("hazard respawn point, just for setting respawn point. non-edit mode player can't see it.", "en-us")]
        [Decoration("IMG_RespawnPoint")]
        public class RespawnTrigger : Resizeable
        {
            private HazardRespawnMarker respawnMarker;
            private void Awake()
            {
                gameObject.transform.localScale *= 2.5f;
                gameObject.layer = (int)GlobalEnums.PhysLayers.PROJECTILES;
                respawnMarker = gameObject.AddComponent<HazardRespawnMarker>();
            }
            private void Start()
            {
                if (!SetupMode)
                    gameObject.GetComponent<SpriteRenderer>().enabled = false;
            }
            private void OnTriggerEnter2D(Collider2D otherCollider)
            {
                int layer = otherCollider.gameObject.layer;
                if (layer == (int)GlobalEnums.PhysLayers.PLAYER || layer == (int)GlobalEnums.PhysLayers.HERO_BOX)
                {
                    PlayerData.instance.SetHazardRespawn(respawnMarker);
                }
            }

        }


        [Description("看起来能破坏的墙壁")]
        [Decoration("HK_break_wall")]
        [Description("breakable wall", "en-us")]
        public class BreakWall : Resizeable
        {
            private void Awake()
            {
                var fsm = gameObject.GetComponent<PlayMakerFSM>();
                fsm.RemoveTransition("Initiate", "ACTIVATE");
                fsm.RemoveAction("Initiate", 11);
                fsm.SetState("Pause");
            }
        }
        [Description("看起来能破坏，但其实不可破坏的墙壁")]
        [Decoration("HK_unbreak_wall")]
        [Description("unbreakbale wall \n it just look like break wall lol", "en-us")]
        public class UnBreakWall : Resizeable
        {
            private void Awake()
            {
                var bw = Instantiate(ObjectLoader.InstantiableObjects["HK_break_wall"]);
                var fsm = bw.GetComponent<PlayMakerFSM>();
                if (fsm)
                {
                    fsm.RemoveTransition("Initiate", "ACTIVATE");
                    fsm.RemoveAction("Initiate", 11);
                    fsm.SetState("Pause");
                    fsm.enabled = false;
                    Destroy(fsm);
                }
                Destroy(bw.GetComponent<CustomDecoration>());
                bw.transform.SetParent(gameObject.transform);
                bw.transform.localPosition = Vector3.zero;
                UnVisableBehaviour.AttackReact.Create(gameObject);
                bw.SetActive(true);
                //Logger.LogDebug(bw.transform.position);
                //bw.AddComponent<ShowColliders>();
            }
        }
        //[Decoration("HK_inspect_region")]
        public class InspectRegion : UnVisableBehaviour
        {
            public void Start()
            {
                gameObject.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmString("Game Text Convo").Value = "Decoration_Test";
            }
        }

        [MemeDecoration]
        [Description("一个不明的法阵，往里面放入某些东西可以激活它")]
        [Description("zote head machine \nplace zote head above can let it work", "en-us")]
        [Decoration("zote_detection")]
        public class ZoteDetection : CustomDecoration
        {
            public Action OpenGate;
            private float dt = 0;
            private const float maxt = 2f;
            private int zotein = 0;
            //magic_circle_b
            private static Sprite unactive;
            private static Sprite active;
            private GameObject go = new GameObject();
            private SpriteRenderer sr
            {
                get
                {
                    var render = go.GetComponent<SpriteRenderer>();
                    if (render == null)
                    {
                        render = go.AddComponent<SpriteRenderer>();
                        go.transform.SetParent(gameObject.transform);
                        go.transform.eulerAngles = new Vector3(-70, 5f, 0);
                        go.transform.localScale = Vector3.one;
                        go.transform.localPosition = Vector3.zero;
                        go.AddComponent<RoteZ>();
                    }
                    return render;
                }
            }
            private class RoteZ : MonoBehaviour
            {
                private void Update()
                {
                    transform.Rotate(new Vector3(0, 0, 1) * 10 * Time.deltaTime);
                }
            }
            private void Awake()
            {
                if (unactive == null || active == null)
                {
                    Texture2D tex;
                    tex = GUIController.Instance.images["magic_circle_b"];
                    unactive = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    tex = GUIController.Instance.images["magic_circle_y"];
                    active = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                sr.sprite = unactive;
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(4f, 0.1f);
                var rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                gameObject.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                var mat = new PhysicsMaterial2D();
                mat.friction = 0.4f;
                mat.bounciness = 0;
                col.sharedMaterial = mat;

                OpenGate = () =>
                {
                    Logger.LogDebug("Zote Open");
                    FindObjectOfType<ZoteWall>()?.Open();
                };
            }
            private void OnCollisionEnter2D(Collision2D collision)
            {

                if (collision.gameObject.name.Contains("ZoteKey"))
                {
                    zotein++;
                    sr.sprite = active;
                }
            }
            private void Update()
            {
                if (zotein > 0)
                {
                    sr.color = Color.Lerp(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), dt / maxt);
                    dt += Time.deltaTime;
                    if (dt >= maxt)
                    {
                        OpenGate?.Invoke();
                        Destroy(gameObject);
                    }
                }
            }
            private void OnCollisionExit2D(Collision2D collision)
            {
                if (collision.gameObject.name.Contains("ZoteKey"))
                {
                    zotein--;
                    sr.color = new Color(1, 1, 1, 1);
                    sr.sprite = unactive;
                    dt = 0;
                }

            }
        }

        [MemeDecoration]
        [Decoration("zote_wall")]
        [Description("佐特之墙，往法阵里放入佐特头骨可以打开（随机打开一个）")]
        [Description("zote wall, can be open by zote head machine", "en-us")]
        public class ZoteWall : Resizeable
        {
            private static AudioClip _hit;
            private static AudioClip _open;
            public static AudioClip zote_hit { get {
                    if (_hit)
                        return _hit;
                    _hit = WavHelper.GetAudioClip("zote_hit");
                    return _hit;
                } }
            public static AudioClip zote_open {
                get
                {
                    if (_open)
                        return _open;
                    _open = WavHelper.GetAudioClip("zote_open");
                    return _open;
                }
            }
            public GameObject head;
            private void Awake()
            {
                var tex = GUIController.Instance.images["ZoteWall"];
                gameObject.AddComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                head = new GameObject();
                head.transform.SetParent(transform);
                head.transform.localPosition = new Vector3(0, 1, 0);
                head.AddComponent<BoxCollider2D>().size = Vector2.one * 0.5f;
                head.AddComponent<HitVoice>();
                head.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                gameObject.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.033173f, 2.238068f);
                col.offset = new Vector2(0.04389954f, -0.2984619f);
                if (SetupMode)
                {
                    //gameObject.AddComponent<ShowColliders>();
                    head.AddComponent<ShowColliders>();
                }
            }
            public void Open()
            {
                Logger.LogDebug("Zote Wall Opened");
                StartCoroutine(Die());
                IEnumerator Die()
                {
                    head.GetComponent<AudioSource>().PlayOneShot(zote_open);
                    yield return new WaitForSeconds(0.6f);
                    Destroy(gameObject);
                }
            }
            private class HitVoice : MonoBehaviour, IHitResponder
            {
                public AudioSource au;
                private void Awake()
                {
                    au = gameObject.AddComponent<AudioSource>();
                }
                public void Hit(HitInstance damageInstance)
                {
                    Logger.LogDebug("zoteHit");
                    au.PlayOneShot(zote_hit);
                }
            }


        }
        [Decoration("lazer_bug")]
        [Description("激光虫，放置的时候只能放在平台的左边沿，因为起始状态是向下爬，\n放其他地方会卡墙卡空气。\n(BUG好难修不管了）")]
        [Description("lazer bug \n it has some issues: unvisuable while placing. \n you can only place it on the left of rect wall", "en-us")]
        public class LazerBug : CustomDecoration
        {
            public static GameObject prefab = ObjectLoader.InstantiableObjects["HK_lazer_bug"];
            private GameObject _l;
            private GameObject lazer {
                get
                {
                    if (_l)
                        return _l;
                    _l = Instantiate(prefab);
                    return _l;
                }
            }
            private void Awake()
            {
                UnVisableBehaviour.AttackReact.Create(gameObject);
                transform.localScale *= 0.5f;
                //lazer = Instantiate(prefab);//,transform);
                lazer.GetComponentInChildren<HealthManager>().hp = 9999;
                //Logger.LogDebug("lazerBUg Spawn" + lazer.name);
                //lazer.GetComponent<HealthManager>().hp = 9999;
                //
            }
            private void Start()
            {
                lazer.transform.position = transform.position;

                lazer.SetActive(true);
            }

            private void OnDestroy()
            {
                Logger.LogDebug("LazerBugDie");
                Destroy(lazer);
            }
        }

        [Decoration("edge")]
        [Description("区域线，可当地板等使用")]
        [Description("edge line. \n has collision \n can think it as floor lol", "en-us")]
        public class LineEdge : Resizeable
        {
            private void Awake()
            {
                var tex = GUIController.Instance.images["lineEdge"];
                var sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                var col = gameObject.AddComponent<EdgeCollider2D>();
                gameObject.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                col.points = new Vector2[] { new Vector2(-0.5f, 0), new Vector2(0.5f, 0) };
            }

            public override void HandleSize(float size)
            {
                gameObject.transform.localScale = new Vector3(size * 10, 2, 1);
            }
        }

        [Obsolete("Use Simple Conveyor instead")]
        [Decoration("HK_Lconveyor")]
        public class LeftConveyor : Resizeable
        {
            private void Awake()
            {
                var go = Instantiate(ObjectLoader.InstantiableObjects["HK_Hconveyor"], transform);
                go.transform.localPosition = new Vector3(0, 2f, -2.4f);

                var cgo = new GameObject("box");
                cgo.transform.SetParent(go.transform);
                cgo.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                cgo.transform.localScale = Vector3.one;

                var col = cgo.AddComponent<BoxCollider2D>();
                var conv = cgo.AddComponent<ConveyorBelt>();
                //conv.speed = -8;
                col.size = new Vector2(10.61741f, 2.202475f);
                col.offset = new Vector2(-0.358706f, -2.418878f);
                col.transform.localPosition = Vector3.zero;

                var mat = new PhysicsMaterial2D();
                mat.friction = 0.2f;
                mat.bounciness = 0;
                col.sharedMaterial = mat;

                go.SetActive(true);

                if (SetupMode)
                    gameObject.AddComponent<ShowColliders>();

            }
            public override void HandleSize(float size)
            {
                gameObject.transform.localScale = new Vector3(size, 1, 1);
            }
            public override void HandleRot(float angle)
            {
            }
            [Handle(Operation.SetSpeed)]
            public void HandleSpeed(int speed)
            {
                var cony = gameObject.GetComponentInChildren<ConveyorBelt>();
                cony.speed = -speed;
            }
            public override void HandleInit(Item i)
            {
                gameObject.SetActive(true);
                base.HandleInit(i);
            }
        }

        [Obsolete("Use Simple Conveyor instead")]
        [Decoration("HK_Rconveyor")]
        public class RightConveyor : Resizeable
        {
            private void Awake()
            {
                var go = Instantiate(ObjectLoader.InstantiableObjects["HK_Hconveyor"], transform);
                go.transform.localPosition = new Vector3(0, -2f, -2.4f);

                var cgo = new GameObject("box");
                cgo.transform.SetParent(go.transform);
                cgo.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                cgo.transform.localScale = Vector3.one;

                var col = cgo.AddComponent<BoxCollider2D>();
                if (gameObject.GetComponent<ConveyorBelt>() == null)
                    cgo.AddComponent<ConveyorBelt>();

                col.size = new Vector2(10.61741f, 2.202475f);
                col.offset = new Vector2(-0.358706f, -2.418878f);
                col.transform.localPosition = Vector3.zero;

                var mat = new PhysicsMaterial2D();
                mat.friction = 0.2f;
                mat.bounciness = 0;
                col.sharedMaterial = mat;

                go.transform.eulerAngles = new Vector3(180, 0, 0);

                go.SetActive(true);
                if (SetupMode)
                    gameObject.AddComponent<ShowColliders>();

            }
            public override void HandleSize(float size)
            {
                gameObject.transform.localScale = new Vector3(size, 1, 1);
            }
            public override void HandleRot(float angle)
            {
            }
            [Handle(Operation.SetSpeed)]
            public void HandleSpeed(int speed)
            {
                var cony = gameObject.GetComponentInChildren<ConveyorBelt>();
                cony.speed = speed;
            }
            public override void HandleInit(Item i)
            {
                gameObject.SetActive(true);
                base.HandleInit(i);
            }
        }

        [Decoration("simple_conveyor")]
        public class SimpleConveyor : Resizeable
        {
            private void Awake()
            {
                GameObject conv = ObjectLoader.InstantiableObjects["HK_Hconveyor"];
                GameObject simple = conv.transform.Find("conveyor_belt_simple0004").gameObject;
                simple = Instantiate(simple);
                simple.transform.SetParent(transform);
                simple.transform.localPosition = Vector3.zero;
                simple.transform.localScale = Vector3.one;
                simple.transform.eulerAngles = transform.eulerAngles;

                gameObject.layer = (int)GlobalEnums.PhysLayers.TERRAIN;
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(4.75f, 0.5f);
                var mat = new PhysicsMaterial2D();
                mat.friction = 0.2f;
                mat.bounciness = 0;
                col.sharedMaterial = mat;
                
                simple.SetActive(true);

                if (SetupMode)
                    gameObject.AddComponent<ShowColliders>();

            }


            [Handle(Operation.SetSpeed)]
            public void HandleSpeed(int speed)
            {
                var cony = gameObject.GetComponent<ConveyorBelt>();
                if(cony == null)
                {
                    cony = gameObject.AddComponent<ConveyorBelt>();
                }

                if (Mathf.Abs(cony.speed) - Mathf.Abs(speed) < 0.1)
                    return;

                cony.speed = -speed;
            }

            public override void HandleSize(float size)
            {
                gameObject.transform.localScale = new Vector3(size*2, 1, 1);
            }

            public override void HandleRot(float angle)
            {
                base.HandleRot(angle);

                var cony = gameObject.GetComponent<ConveyorBelt>();
                if (cony == null)
                {
                    cony = gameObject.AddComponent<ConveyorBelt>();
                }

                if (approach(angle,90) || approach(angle,270))
                {
                    cony.vertical = true;
                }
                else
                {
                    cony.vertical = false;
                }
                if (approach(angle, 270) || approach(angle, 180))
                {
                    cony.speed = ((ItemDef.ConveyorItem)item).speed;
                }
                else
                {
                    cony.speed = -((ItemDef.ConveyorItem)item).speed;
                }
            }
        
            private bool approach(float number,float target,float delta=10f)
            {
                return Mathf.Abs(target - number) < delta;
            }
        }

        [Decoration("stomper_switch")]
        [Description("A switch for toggle stomper on/off ","en-us")]
        public class ToggleStomper :Resizeable
        {
            private void Awake()
            {
                var lever = ObjectLoader.InstantiableObjects["HK_lift_lever"];
                lever = Instantiate(lever);
                lever.transform.SetParent(transform);
                lever.transform.localPosition = Vector3.zero;
                var fsm = lever.LocateMyFSM("Call Lever");
                fsm.RemoveAction("Send Msg", 0);
                fsm.RemoveAction("Send Msg", 0);
                fsm.RemoveAction("Send Msg", 0);
                fsm.InsertMethod("Send Msg", 0, Toggle);
                fsm.ChangeTransition("Left", "FINISHED", "Send Msg");
                fsm.ChangeTransition("Right", "FINISHED", "Send Msg");

                lever.GetComponentInChildren<tk2dSprite>().color = new Color(1, 0, 1);

                lever.SetActive(true);
            }
            private void Toggle()
            {
                //Logger.Log("[Toggle] Hello World!");
                var animators = FindObjectsOfType<Animator>().Where(x => x.gameObject.name.Contains("mines_stomper"));
                foreach(var ani in animators)
                {
                    ani.enabled = !ani.enabled;
                }
            }
        }

        [Decoration("white_thorn")]
        public class Thorn : Resizeable
        {
            private void Awake()
            {
                var sr = gameObject.AddComponent<SpriteRenderer>();
                var col = gameObject.AddComponent<EdgeCollider2D>();
                gameObject.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                var dmg = gameObject.AddComponent<DamageHero>();
                dmg.damageDealt = 1;
                dmg.hazardType = (int)GlobalEnums.HazardType.ACID;
                dmg.shadowDashHazard = false;
                var tex = GUIController.Instance.images["w_thorn"];
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

                col.points = new Vector2[] { 
                    new Vector2(-2.349958f,-0.8096175f),
                    new Vector2(-1.926968f,0.04194808f),
                    new Vector2(-0.9399956f,0.4974908f),
                    new Vector2(0.3034189f,0.8203003f),
                    new Vector2(1.505731f,0.5203433f),
                    new Vector2(2.402526f,-0.4310951f),
                };
                gameObject.AddComponent<NonBouncer>();
            }
        }

        [Decoration("white_spike")]
        public class WhiteSpike : Resizeable
        {
            private void Awake()
            {
                var sr = gameObject.AddComponent<SpriteRenderer>();
                var col = gameObject.AddComponent<BoxCollider2D>();
                gameObject.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                var dmg = gameObject.AddComponent<DamageHero>();
                dmg.damageDealt = 1;
                dmg.hazardType = (int)GlobalEnums.HazardType.ACID;
                dmg.shadowDashHazard = false;
                var tex = GUIController.Instance.images["w_spike"];
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                col.size = Vector2.one;
                gameObject.AddComponent<TinkEffect>().blockEffect = ObjectLoader.InstantiableObjects["HK_saw"].GetComponent<TinkEffect>().blockEffect;
                    
            }
        }

        [Description("填充背景用的色块，大概")]
        [Decoration("back_colorfull_fill")]
        public class BackColorFill : PartResizable
        {
            [Serializable]
            public class PartResizeColor : ColorItem
            {
                [Handle(Operation.SetSizeX)]
                [FloatConstraint(0.2f, 2f)]
                public float size_x { get; set; } = 1;

                [Handle(Operation.SetSizeY)]
                [FloatConstraint(0.2f, 2f)]
                public float size_y { get; set; } = 1;

                [Handle(Operation.SetRot)]
                [IntConstraint(0, 360)]
                public int angle { get; set; } = 0;

                [Handle(Operation.SetOrder)]
                [IntConstraint(-10,100)]
                public int Order { get; set; } = 0;

                [InspectIgnore]
                public override float A { get; set; }
                
            }
            private SpriteRenderer sr { get {
                    var s = gameObject.GetComponent<SpriteRenderer>();
                    if (s)
                        return s;
                    return gameObject.AddComponent<SpriteRenderer>();
                } }
            public const int factor = 400;
            public override void HandleSizeX(float size)
            {
                base.HandleSizeX(size*factor);
            }
            public override void HandleSizeY(float size)
            {
                base.HandleSizeY(size*factor);
            }

            private void Awake()
            {
                UnVisableBehaviour.AttackReact.Create(gameObject);
            }

            [Handle(Operation.SetColorR)]
            [Handle(Operation.SetColorG)]
            [Handle(Operation.SetColorB)]
            //[Handle(Operation.SetColorA)]
            public void HandleColors(float val)
            {
                if(sr.sprite == null)
                {
                    sr.sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
                    sr.material = new Material(Shader.Find("Unlit/Texture"));
                }

                var c = ((PartResizeColor)item).GetColor();
                sr.sprite.texture.SetPixels(new Color[] { c });
                sr.sprite.texture.Apply();
            }
            [Handle(Operation.SetOrder)]
            public void HandleOrder(int val)
            {
                sr.sortingOrder = val;
            }
        }

        [Decoration("HK_shadow_gate")]
        public class ShadowGate : Resizeable
        {
            public override void HandleSize(float size)
            {
                gameObject.transform.localScale = new Vector3(1, size, 1);
            }
        }
    
        [Decoration("hazard_saver")]
        [Description("临时重生点，离开范围恢复原本的")]
        [Description("A platform that only spawns when the knight hits a hazard, then goes away","en-us")]
        public class HazardSaver : Resizeable
        {
            public Vector2 DefaultSize = Vector2.one * 10;
            public Vector2 selfHazardPos => transform.position;
            public Vector2 LastHazardPos;
            private BoxCollider2D col;
            public override void HandleSize(float size)
            {
                if(col == null)
                    col = gameObject.AddComponent<BoxCollider2D>();
                col.size = DefaultSize * size;
            }
            public override void Remove(object self = null)
            {
                Exit();
                base.Remove(self);
            }
            private void Awake()
            {
                if(col == null)
                    col = gameObject.AddComponent<BoxCollider2D>();
                gameObject.layer = (int)GlobalEnums.PhysLayers.PROJECTILES;
                
                if(SetupMode)
                    gameObject.AddComponent<ShowColliders>();

                var ht = gameObject.AddComponent<HeroTrigger>();
                ht.HeroEnter = Enter;
                ht.HeroExit = Exit;
                
            }
            private void Enter()
            {
                //On.HeroController.HazardRespawn += HeroController_HazardRespawn;
                On.GameManager.HazardRespawn += GameManager_HazardRespawn;
                LastHazardPos = PlayerData.instance.hazardRespawnLocation;
                HeroController.instance.SetHazardRespawn(selfHazardPos,true);

                Logger.LogDebug("Enter");
            }
            private void OnDestroy()
            {
                Exit();
            }
            private void GameManager_HazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self)
            {
                SpawnPlatform();
                orig(self);
            }

            private void Exit()
            {
                On.GameManager.HazardRespawn -= GameManager_HazardRespawn;
                if ((Vector2)PlayerData.instance.hazardRespawnLocation == selfHazardPos)
                    HeroController.instance.SetHazardRespawn(LastHazardPos, true);

                Logger.LogDebug("Exit");
            }
            private void SpawnPlatform()
            {
                var plat = new GameObject();
                plat.AddComponent<TempPlatform>();
                plat.layer = 8;
                plat.transform.position = selfHazardPos + new Vector2(0, -1);
                plat.SetActive(true);
                plat.transform.localScale *= 0.8f;
                Logger.LogDebug("Spawn Hazard Platform");
            }
            
            private class TempPlatform : MonoBehaviour
            {
                private float dt = 3;
                private float t = 0;
                private SpriteRenderer sr;
                private BoxCollider2D col;
                private void Awake()
                {
                    sr = gameObject.AddComponent<SpriteRenderer>();
                    var tex = GUIController.Instance.images["seal_wall"];
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    col = gameObject.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(1, 4.5f);
                    gameObject.layer = 8;
                    gameObject.transform.eulerAngles = new Vector3(0, 0, 90*3);
                }
                private void Update()
                {
                    t += Time.deltaTime;
                    sr.color = new Color(1, 1, 1, (dt - t) / dt);
                    if (dt - t < 0.1)
                    {
                        Destroy(gameObject);
                    }

                }
            }
        }
    
        [Decoration("jarcol_floor")]
        [Description("A floor","en-us")]
        public class JarcolFloor:Resizeable
        {
            private void Awake()
            {
                var sr = gameObject.AddComponent<SpriteRenderer>();
                var col = gameObject.AddComponent<BoxCollider2D>();
                var tex = GUIController.Instance.images["jarcol_floor"];
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                col.size = new Vector2(11.30954f, 0.8839264f);
                col.offset = new Vector2(0.007995605f, 0.1063919f);
                gameObject.layer = 8;
                
            }
        }
    }
}
