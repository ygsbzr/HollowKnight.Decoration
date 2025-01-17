﻿using DecorationMaster.Attr;
using DecorationMaster.MyBehaviour;
using DecorationMaster.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using HutongGames.PlayMaker.Actions;
namespace DecorationMaster
{
    // Create a objectpool name InstantiableObjects which can be access with name
    // but those object are not contains custom monobehaviour
    // to add behaviour, you should make a class with DecorationAttribute which value is name, and then
    // register will automatically add these components to pool prefab
    // 
    // objectname with a prefix "HK_"
    public static partial class ObjectLoader
    {
        
        public static readonly Dictionary<(string, Func<GameObject, GameObject>), (string, string)> ObjectList = new Dictionary<(string, Func<GameObject, GameObject>), (string, string)>
        {
            
            {
                ("lift_lever",null),("Ruins1_05","Lift Call Lever (2)")
            },
            {
                ("stomper",null),
                ("Mines_19","_Scenery/stomper_1")
            },
            
            {
                ("Hconveyor", (go) =>
                {
                    go.transform.localScale = Vector3.one;
                    return go;
                }

                ),("Mines_31","conveyor_belt_0mid (3)")
            },
            {("lore_tablet_1",(go)=>{
                //go.GetComponent<BoxCollider2D>().offset= new Vector2(-0.2f,-1);
                var lit = go.transform.Find("lit_tablet").gameObject;
                var sprite = lit.GetComponent<SpriteRenderer>().sprite;
                var ngo = new GameObject();
                ngo.AddComponent<SpriteRenderer>().sprite = sprite;
                var localPos = lit.transform.localPosition;
                lit.transform.SetParent(null);
                ngo.transform.SetParent(go.transform);
                ngo.transform.localPosition = localPos;
                Object.Destroy(lit);
                go.GetComponent<BoxCollider2D>().offset = new Vector2(-0.2f,-1);
                go.GetComponent<BoxCollider2D>().size = new Vector2(4,2);
                return go;
            }),("Tutorial_01","_Props/Tut_tablet_top (1)") },
            
            {("inspect_region",null),("White_Palace_18","Inspect Region")},
            {("garden_plat_s",null),("Fungus3_13",("Royal Gardens Plat S")) },
            {("crystal_dropping",null),("Mines_31","Pt Crystal Dropping (13)")},
            {("zap_cloud",null),("Fungus3_archive_02","Zap Cloud") },
            {("bench",null),("Crossroads_47","RestBench") },
            {("quake_floor",null),("Crossroads_52", "Quake Floor") },
            {("shadow_gate",(go)=>{
                foreach(Transform t in go.transform)
                {
                    if(t.name.Contains("prongs"))
                        Object.Destroy(t.gameObject);
                }
                return go;
            }),("Fungus3_44", "shadow_gate") },
            {("zote_head",(go)=>{go.name = "ZoteKey";return go; }),("Fungus1_20_v02","Zote Death/Head") },
            {
                ("saw", (go)=>{
                    go.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                    
                    return go;
                }),
                ("White_Palace_18","saw_collection/wp_saw")
            },
            
            {("laser_turret", (go)=>{
                var fsm = go.LocateMyFSM("Laser Bug");
                fsm.AddAction
                (
                    "Init",
                    new WaitRandom
                    {
                        timeMax = 1f,
                        timeMin = 0
                    }
                );
                fsm.Fsm.SaveActions();
                return go;
            }), ("Mines_31", "Laser Turret") },
            {
                ("infinte_soul",null),
                ("White_Palace_18","Soul Totem white_Infinte")
            },
            { 
                ("trap_spike",(go)=>{
                    go.transform.Find("wp_anim_spikes_fast").gameObject.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                    return go;
                }),("White_Palace_07","wp_trap_spikes")
            },
            {
                ("flip_platform",null),("Mines_31","Mines Platform")
            },
            {
                ("gate",(go)=>{
                    go.LocateMyFSM("Toll Gate").SetState("Idle");
                    return go;
                }),
                ("Crossroads_03", "_Props/Toll Gate")
            },
            {
                ("lever",(go)=>{
                    PlayMakerFSM playMakerFSM = go.LocateMyFSM("toll switch");
                    FsmutilExt.RemoveAction(playMakerFSM, "Initiate", 4);
                    FsmutilExt.RemoveAction(playMakerFSM, "Initiate", 3);
                    playMakerFSM.SetState("Pause");
                    playMakerFSM.RemoveTransition("Initiate","ACTIVATED");
                    playMakerFSM.Fsm.SaveActions();
                    return go;
                }),
                ("Crossroads_03", "_Props/Toll Gate Switch")
            },
            {
                ("spike",null),
                ("Crossroads_25","Cave Spikes tile")
            },
            {
                ("soul_totem",null),
                ("Crossroads_25","Soul Totem mini_two_horned")
            },
            {
                ("lazer_bug",null),
                ("Mines_05","Crystallised Lazer Bug")
            },
            {
                ("platform_rect",null),
                ("Mines_05","plat_float_08")
            },
            {
                ("crystal_barrel",null),
                ("Mines_05","crystal_barrel")
            },
            {
                ("platform_small",null),
                ("Mines_05","plat_float_03")
            },
            {
                ("crystal",null),
                ("Mines_05","brk_Crystal3")
            },
            {
                ("fly",null), ("White_Palace_18","White Palace Fly")
            },
            {
                ("bounce_shroom",null),
                ("Fungus2_11","Bounce Shroom B")
            },
            {
                ("turret",null),
                ("Fungus2_11","Mushroom Turret")
            },
            {
                ("break_wall",null),
                ("Crossroads_03","_Scenery/Break Wall 2")
            }
            /*
            {
                ("cameralock",null),
                ("Crossroads_25","CameraLockArea")
            },
            */

            
        };
        public static Dictionary<string, GameObject> InstantiableObjects { get; } = new Dictionary<string, GameObject>();
        public static GameObject CloneDecoration(string key,Item exists = null)
        {
            GameObject go = null;
            if(InstantiableObjects.TryGetValue(key,out GameObject prefab))
            {
                Item prefab_item = prefab.GetComponent<CustomDecoration>()?.item;
                if (prefab_item == null)
                    return null;
                go = Object.Instantiate(prefab);
                go.name = go.name.Replace("(Clone)", "");
                go.GetComponent<CustomDecoration>().Setup(Operation.Serialize, exists == null?prefab_item.Clone():exists);
            }
            return go;
        }
        public static GameObject CloneDecoration(Item prefab) => CloneDecoration(prefab.pname, prefab);
        public static void Load(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            static GameObject Spawnable(GameObject obj, Func<GameObject, GameObject> modify)
            {
                GameObject go = Object.Instantiate(obj);
                go = modify?.Invoke(go) ?? go;
                Object.DontDestroyOnLoad(go);
                go.transform.localScale = Vector3.one;
                go.SetActive(false);
                return go;
            }

            foreach (var ((name, modify), (room, go_name)) in ObjectList)
            {
                if (!preloadedObjects[room].TryGetValue(go_name, out GameObject go))
                {
                    Logger.LogWarn($"[DecorationMaster]: Unable to load GameObject {go_name}");

                    continue;
                }

                InstantiableObjects.Add($"HK_{name}", Spawnable(go, modify));
            }

            static GameObject ImageSpawnable(string imgN)
            {
                var imggo = ImageLoader.CreateImageGo(imgN);
                Object.DontDestroyOnLoad(imggo);
                imggo.transform.localScale = Vector3.one;
                imggo.SetActive(false);
                return imggo;
            }
            ImageLoader.Load();
            foreach(var imgName in ImageLoader.images.Keys)
            {
                InstantiableObjects.Add($"IMG_{imgName}", ImageSpawnable(imgName));
            }

            Logger.LogDebug($"ObjectLoader: Load done,Count:{InstantiableObjects.Count}");
            foreach(var k in InstantiableObjects.Keys)
            {
                Logger.LogDebug(k);
            }
        }
        public static class ImageLoader
        {
            public static readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
            public static readonly Dictionary<string, byte[]> raw_images = new Dictionary<string, byte[]>();
            public static bool loaded { get; private set; }
            public static void Load()
            {
                if (loaded)
                    return;

                string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                foreach (string res in resourceNames)
                {
                    Logger.LogDebug($"Find Embeded Resource:{res}");
                    if (res.EndsWith(".png"))
                    {
                        try
                        {
                            Stream imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                            byte[] buffer = new byte[imageStream.Length];
                            imageStream.Read(buffer, 0, buffer.Length);
                            string[] split = res.Split('.');
                            string internalName = split[split.Length - 2];

                            if (res.Contains("images.objects"))
                            {
                                Texture2D tex = new Texture2D(1, 1);
                                tex.LoadImage(buffer.ToArray());
                                images.Add(internalName, tex);
                            }
                            else
                            {
                                raw_images.Add(internalName, buffer);
                            }
                        }
                        catch
                        {
                            loaded = false;
                        }
                    }
                }
                loaded = true;
            }
            public static GameObject CreateDestroyableTex(Texture2D tex)
            {
                var go = new GameObject { name = "DestroyableTexture" };
                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                go.SetActive(false);
                go.AddComponent<SpriteRenderer>().sprite = sprite;
                go.AddComponent<BoxCollider2D>().size = Vector2.one;
                go.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
                go.AddComponent<NonBouncer>();
                //To Add some component HERE
                return go;
            }
            public static GameObject CreateImageGo(string texName)
            {
                if (!images.ContainsKey(texName))
                    return null;

                var tex = images[texName];
                var go = CreateDestroyableTex(tex);

                go.name = $"D_IMG_{texName}";
                return go;
            }
        }
        
    }

    // Processor for Instantiate Object
    // To Register a Behaviour means add behaviour to object pool or create an empty object with this behaviour
    public static class BehaviourProcessor
    {
        public static void Register<TDec,TItem>(string poolname) where TDec : CustomDecoration where TItem : Item
        {
            Register(poolname, typeof(TDec), typeof(TItem));
        }
        public static void Register<TDec>(string poolname,Type ti) where TDec : CustomDecoration
        {
            Register(poolname, typeof(TDec), ti);
        }
        public static void Register(string poolname,Type td,Type ti)
        {
            if (!(td.IsSubclassOf(typeof(CustomDecoration))) || !(ti.IsSubclassOf(typeof(Item))))
                throw new ArgumentException($"{td}-{ti} match exception");


            if (!ObjectLoader.InstantiableObjects.ContainsKey(poolname)) // create an empty gameobject for registion
            {
                GameObject empty = new GameObject();
                Object.DontDestroyOnLoad(empty);
                empty.SetActive(false);
                ObjectLoader.InstantiableObjects.Add(poolname, empty);
                Logger.LogDebug($"Cant find an object in InstantiableObjects, create an empty GO instead");
            }

            GameObject prefab = ObjectLoader.InstantiableObjects[poolname];
            var item = Activator.CreateInstance(ti) as Item;
            item.pname = poolname;
            CustomDecoration d = prefab.AddComponent(td) as CustomDecoration;
            d.item = item;

            Logger.Log($"Register [{poolname}] - Behaviour : {td} - DataStruct : {ti}");
            ReflectionCache.GetItemProps(ti, Operation.None);
            ReflectionCache.GetMethods(td, Operation.None);
            //ItemDescriptor.Register(td,poolname);

            #region add addition item
            var additions = td.GetCustomAttributes(typeof(AdditionItemAttribute), true).OfType<AdditionItemAttribute>().Select(x => x.type);
            foreach(var a in additions)
            {
                if (a == ti)
                    continue;
                RegisterAddition(item, a);
                Logger.LogDebug($"[{td}] Reg addition item {a}");
            }
            #endregion
        }

        private static void RegisterAddition(Item item,Type ti)
        {
            //if(!ti.IsSubclassOf(typeof(Item)))
            //{
            //    throw new ArgumentException("Try to add Error item");
            //}
            //var subitem = Activator.CreateInstance(ti) as Item;
            //item.additionItem.Add(subitem);
            
        }
        //T is a class which includes a lot of sub-class in type CustomDecoration
        public static void RegisterBehaviour<T>()
        {
            var behaviours = typeof(T).GetNestedTypes(BindingFlags.Public).Where(x => x.IsSubclassOf(typeof(CustomDecoration)));
            var items = typeof(ItemDef).GetNestedTypes(BindingFlags.Public | BindingFlags.Instance).Where(x => x.IsSubclassOf(typeof(Item)));

            foreach (Type b in behaviours)
            {
                DecorationAttribute attr = b.GetCustomAttributes(typeof(DecorationAttribute), false).OfType<DecorationAttribute>().FirstOrDefault();
                if (attr == null)
                    continue;

                string poolname = attr.Name;

                Type DataStruct = null;
                foreach (Type i in items) // Search Item Defination in ItemDef
                {
                    DecorationAttribute[] i_attr = i.GetCustomAttributes(typeof(DecorationAttribute), false).OfType<DecorationAttribute>().ToArray();
                    if (i_attr == null || i_attr.Length == 0)
                        continue;
                        
                    if(i_attr.Contains(attr))
                    {
                        DataStruct = i;
                        break;
                    }
                }
                if(DataStruct == null) // Search Item Defination in Behaviour
                {
                    DataStruct = b.GetNestedTypes(BindingFlags.Public).Where(x => x.IsSubclassOf(typeof(Item))).FirstOrDefault();
                }
                if (DataStruct == null) // Search Item Defination in T
                {
                    DataStruct = typeof(T).GetNestedTypes(BindingFlags.Public).Where(x => x.IsSubclassOf(typeof(Item))).FirstOrDefault();
                }
                if (DataStruct == null) // Fill with defatult Item
                {
                    Logger.LogWarn($"Could Not Found an Item that match {b.FullName},Attr:{attr.Name},will use default item instance");
                    DataStruct = typeof(ItemDef.DefaultItem);
                }

                Register(poolname, b, DataStruct);
            }
        }
        public static void RegisterSharedBehaviour<T>() where T : CustomDecoration
        {
            var shareAttr = typeof(T).GetCustomAttributes(typeof(DecorationAttribute), false).OfType<DecorationAttribute>();
            foreach(var attr in shareAttr)
            {
                if (attr == null)
                    continue;
                string poolname = attr.Name;

                var ti = typeof(T).GetNestedTypes(BindingFlags.Public).Where(x => x.IsSubclassOf(typeof(Item))).FirstOrDefault();

                Register<T>(poolname, ti);
            }
        }
    }
}
