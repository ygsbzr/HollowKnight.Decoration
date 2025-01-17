﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DecorationMaster.MyBehaviour;
using HutongGames.PlayMaker;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using DecorationMaster.UI;
using DecorationMaster.Util;
using DecorationMaster.MyBehaviour.Gem;
namespace DecorationMaster
{
    public delegate int SelectItem();
    public class DecorationMaster : Mod,ITogglableMod,IGlobalSettings<GlobalModSettings>
    {
        private float autoSaveTimer = 0;
        private static GameManager _gm;
        private GameObject UIObj;
        private Vector2 mousePos;
        private GameObject _current_respawn = null;

        public static DecorationMaster instance;
        public SelectItem SelectGetter;
        internal static GameManager GM => _gm != null ? _gm : (_gm = GameManager.instance);
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            instance = this;



            #region VerifyVersion
            Logger.Log("Load Global Json");
            ItemSettings global = SerializeHelper.LoadGlobalSettings<ItemSettings>();
            if (global != null)
            {
                if (global.mod_version > Version)
                {
                    new ErrorPanel($"Require Version:{global.mod_version},BUT you Version:{Version}\n(你的MOD版本该更新了)");
                    throw new FileLoadException("Try To Load an newer json data in an older mod,please update mod");
                }
                    
                ItemData = global;
                Logger.Log("Loaded Json");
                
            }
            #endregion

            #region Init GameObject
            ObjectLoader.Load(preloadedObjects);
            BehaviourProcessor.RegisterBehaviour<Particle>();
            BehaviourProcessor.RegisterBehaviour<Draw>();
            BehaviourProcessor.RegisterBehaviour<OtherBehaviour>();
            BehaviourProcessor.RegisterBehaviour<AreaBehaviour>();
            BehaviourProcessor.RegisterBehaviour<MovablePlatform>();
            BehaviourProcessor.RegisterBehaviour<ModifyGameItem>();
            BehaviourProcessor.RegisterBehaviour<Mana>();
            BehaviourProcessor.RegisterBehaviour<AudioBehaviours>();
            BehaviourProcessor.RegisterBehaviour<OneShotBehaviour>();
            BehaviourProcessor.RegisterBehaviour<Scope>();
            BehaviourProcessor.RegisterBehaviour<Bench>();
            BehaviourProcessor.RegisterSharedBehaviour<DefaultBehaviour>();
            BehaviourProcessor.RegisterSharedBehaviour<UnVisableBehaviour>();
            BehaviourProcessor.RegisterSharedBehaviour<DelayResizableBehaviour>();
            BehaviourProcessor.RegisterSharedBehaviour<TransitionGem>();
            #endregion

            #region InitGUI
            UIObj = new GameObject();
            UIObj.AddComponent<GUIController>();
            UnityEngine.Object.DontDestroyOnLoad(UIObj);
            GUIController.Instance.BuildMenus();
            #endregion

            #region SetupCallBack
            SelectGetter = GetKeyPress;
            SelectGetter += PickPanel.SelectFocus;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SpawnFromSettings;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += ShowRespawn;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += AutoSaveModification;
            On.GameManager.PositionHeroAtSceneEntrance += HeroOutBoundSave;
            ModHooks.LanguageGetHook += DLanguage.MyLanguage;
            ModHooks.ApplicationQuitHook += SaveJson;
            if (Settings.CreateMode)
            {
                ModHooks.HeroUpdateHook += OperateItem;
                if(Settings.ShowRespawnPoint)
                {
                    On.PlayerData.SetHazardRespawn_HazardRespawnMarker += ShowCurrentRespawnPoint;
                    On.PlayerData.SetHazardRespawn_Vector3_bool += ShowCurrentRespawnPoint;
                }
            }
                
            #endregion

            UserLicense.ShowLicense();
            
        }

        private void ShowCurrentRespawnPoint(Vector3 position)
        {
            if (HeroController.instance == null)
                return;

            LogDebug("Reflesh HzRespawn "+position.ToString());

            UnityEngine.Object.Destroy(_current_respawn);

            //GUIController.Instance.images["lineEdge"];
            _current_respawn = new GameObject("HzShow");
            var tex = GUIController.Instance.images["knight_idle"];
            _current_respawn.AddComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            //_current_respawn.transform.SetPosition2D(HeroController.instance.FindGroundPoint(position, true));
            NameDisp.Create(_current_respawn, "spawn here");

            RaycastHit2D raycastHit2D = Physics2D.Raycast(position, Vector2.down, 50, 256);
            _current_respawn.transform.SetPosition2D(raycastHit2D.point.x,raycastHit2D.point.y+0.7f);

        }

        private void ShowCurrentRespawnPoint(On.PlayerData.orig_SetHazardRespawn_Vector3_bool orig, PlayerData self, Vector3 position, bool facingRight)
        {
            orig(self, position, facingRight);
            ShowCurrentRespawnPoint(position);
        }

        private void ShowCurrentRespawnPoint(On.PlayerData.orig_SetHazardRespawn_HazardRespawnMarker orig, PlayerData self, HazardRespawnMarker location)
        {
            orig(self, location);
            ShowCurrentRespawnPoint(location.transform.position);
        }

        private void HeroOutBoundSave(On.GameManager.orig_PositionHeroAtSceneEntrance orig, GameManager self)
        {
            orig(self);
            if(HeroController.instance.transform.position.x<-19900)
            {
                GameManager.instance.StartCoroutine(_respawn());
                LogDebug("Save Hero");
            }
            IEnumerator _respawn()
            {
                var bench = GameObject.FindGameObjectWithTag("RespawnPoint");
                var trigger = UnityEngine.Object.FindObjectOfType<HazardRespawnTrigger>();
                yield return new WaitForSeconds(1f);

                if (HeroController.instance.transform.position.x < -19900)
                {
                    if(bench!=null)
                    {
                        HeroController.instance.SetHazardRespawn(bench.transform.position, true);
                    }
                    else if(trigger != null)
                    {
                        PlayerData.instance.SetHazardRespawn(trigger.respawnMarker);
                    }
                    else
                    {
                        LogError("Can't Respawn currectly");
                    }
                    Respawn();
                }
               
            }
        }
        public static void Respawn()
        {
            if (GameManager.instance.IsGameplayScene() && !HeroController.instance.cState.dead && PlayerData.instance.health > 0)
            {
                if (UIManager.instance.uiState.ToString() == "PAUSED")
                {
                    UIManager.instance.TogglePauseGame();
                    GameManager.instance.HazardRespawn();
                    
                    return;
                }
                if (UIManager.instance.uiState.ToString() == "PLAYING")
                {
                    HeroController.instance.RelinquishControl();
                    GameManager.instance.HazardRespawn();
                    HeroController.instance.RegainControl();

                    return;
                }
            }
        }
        private void AutoSaveModification(Scene arg0, LoadSceneMode arg1)
        {
            if(SceneItemData.TryGetValue(arg0.name,out var setting))
            {
                setting.AutoSave();
            }
        }

        private void SaveJson()
        {
            ItemData.mod_version = Version;
            SerializeHelper.SaveGlobalSettings(ItemData);
            if(GM!=null)
            {
                if (SceneItemData.TryGetValue(GM.sceneName, out var currentSetting))
                    currentSetting.AutoSave();
            }
            Logger.LogDebug("Save Global Json");
        }

        private void ShowRespawn(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name.Contains("Menu_Title"))
                return;
            if (!ItemManager.Instance.setupMode)
                return;
            GameManager.instance.StartCoroutine(WaitSceneLoad(arg0));
            IEnumerator WaitSceneLoad(Scene arg0)
            {
                yield return new WaitUntil(() => (arg0.isLoaded));
                var triggers = UnityEngine.Object.FindObjectsOfType<HazardRespawnTrigger>();
                foreach(var t in triggers)
                {
                    t.gameObject.AddComponent<ShowColliders>();
                }
                Logger.LogDebug($"found respawn :{triggers.Length}");
            }
        }

        // Spawn Object from Json files 
        private void SpawnFromSettings(Scene arg0, LoadSceneMode arg1)
        {
            //Logger.LogDebug($"Item Count:{ItemData.items.Count}");
            if (arg0.name.Contains("Menu_Title"))
                return;
            
            IEnumerator SpawnGlobal(Scene arg0)
            {
                int count = 0;
                Logger.LogDebug("Try to spawn setting");
                string sceneName = arg0.name;
                yield return new WaitUntil(() => (arg0.isLoaded));
                var spawnlist = ItemData.items.Where(x => x.sceneName == sceneName).ToArray();
                foreach (var r in spawnlist)
                {
                    try
                    {
                        if (ObjectLoader.CloneDecoration(r) != null)
                            count++;
                    }
                    catch
                    {
                        Logger.LogError($"Spawn Failed When Try to Spawn {r?.pname}");
                    }
                }
                Modding.Logger.LogDebug($"All Fine,Spawn {count} in {sceneName}");
                yield break;
            }
            IEnumerator SpawnLocal(Scene arg0,ItemSettings setting)
            {
                int count = 0;
                Logger.LogDebug("Try to spawn setting");
                string sceneName = arg0.name;
                yield return new WaitUntil(() => (arg0.isLoaded));
                var spawnlist = setting.items;
                foreach (var r in spawnlist)
                {
                    try
                    {
                        if (ObjectLoader.CloneDecoration(r) != null)
                            count++;
                    }
                    catch
                    {
                        Logger.LogError($"Spawn Failed When Try to Spawn {r?.pname}");
                    }
                }
                Modding.Logger.LogDebug($"All Fine,Spawn {count} in {sceneName}");
                yield break;
            }

            if (ItemData.items.Count > 0)
            {
                GameManager.instance.StartCoroutine(SpawnGlobal(arg0));
            }
            if (SceneItemData.TryGetValue(arg0.name,out var sceneSetting))
            {
                GameManager.instance.StartCoroutine(SpawnLocal(arg0, sceneSetting));
            }
            else
            {
                var _scene_setting = SerializeHelper.LoadSceneSettings<ItemSettings>(arg0.name);
                if(_scene_setting != null)
                {
                    SceneItemData.Add(arg0.name, _scene_setting);
                    GameManager.instance.StartCoroutine(SpawnLocal(arg0, _scene_setting));
                }
            }


        }

        
        private void OperateItem() //Hero update op
        {
            if(GM.isPaused || GM.IsInSceneTransition)
            {
                return;
            }
            if(ItemManager.Instance.setupMode)
            {
                autoSaveTimer += Time.deltaTime;
                if(autoSaveTimer > 60*3)
                {
                    SaveJson();
                    autoSaveTimer = 0;
                    
                }
            }
            if((Input.GetKey(KeyCode.LeftControl)|| Input.GetKey(KeyCode.RightControl))) // ctrl组合键
            {
                if (Input.GetKeyDown(KeyCode.C))
                    ItemManager.Instance.CopyBlock();
                else if (Input.GetKeyDown(KeyCode.M))
                    ItemManager.Instance.MoveBlock();
                else if (Input.GetKeyDown(KeyCode.D))
                    ItemManager.Instance.DelBlock();
                else if (Input.GetKeyDown(KeyCode.Z))
                    ItemManager.Instance.DiscardLast();
            }

            if (Input.GetKeyDown(ToggleEdit))    // Toggle Edit Model
            {
                ItemManager.Instance.ToggleSetup();
            }
            if (Input.GetKeyDown(SwitchGroup))   // Switch Select Group
            {
                ItemManager.Instance.SwitchGroup();
            }

            //Vector2 cur_mousePos = GetMousePos();   //Update Mouse Pos
            Vector2 cur_mousePos = MyCursor.CursorPosition;
            if (cur_mousePos != mousePos)
            {
                mousePos = cur_mousePos;
                ItemManager.Instance.Operate(Operation.SetPos, mousePos);
            }

            if(GM != null)
            {
                if (Input.GetMouseButtonUp((int)MouseButton.Left)) // Confirm Go
                {
                    ItemManager.Instance.AddCurrent();
                }
                else if (Input.GetMouseButtonUp((int)MouseButton.Right)) // Discard Go
                {
                    ItemManager.Instance.RemoveCurrent();
                }
            }
            int idx = -1;
            foreach (SelectItem selector in SelectGetter.GetInvocationList())// Get user Selection
            {
                int res = selector.Invoke();
                if (res != -1)
                    idx = res;
            }
            if (ItemManager.Instance.Select(idx) == null)
                return;

        }

        private static int GetKeyPress()
        {
            for(int i=1;i<= ItemManager.GroupMax; i++)
            {
                if( Input.GetKeyDown(KeyCode.Alpha0 + i) )
                {
                    return i;
                }
                    
            }
           
            return -1;
        }
        public static Vector3 GetMousePos()
        {
            //var screenPos = Camera.main.WorldToScreenPoint(HeroController.instance.transform.position);
            var screenPos = Camera.main.WorldToScreenPoint(new Vector3(0,0,0));
            var mousePosOnScreen = Input.mousePosition;
            mousePosOnScreen.z = screenPos.z;
            return Camera.main.ScreenToWorldPoint(mousePosOnScreen);
        }
        public void OnLoadGlobal(GlobalModSettings s) => Settings = s;
        public GlobalModSettings OnSaveGlobal() => Settings;

        public void Unload()
        {
            SaveJson();
            #region RemoveCallBack
            SelectGetter = null;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SpawnFromSettings;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= ShowRespawn;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AutoSaveModification;
            ModHooks.LanguageGetHook -= DLanguage.MyLanguage;
            ModHooks.ApplicationQuitHook -= SaveJson;

            ModHooks.HeroUpdateHook -= OperateItem;
            On.PlayerData.SetHazardRespawn_HazardRespawnMarker -= ShowCurrentRespawnPoint;
            On.PlayerData.SetHazardRespawn_Vector3_bool -= ShowCurrentRespawnPoint;
            UnityEngine.Object.Destroy(UIObj);
            #endregion

        }

        public GlobalModSettings Settings = new GlobalModSettings();
        public ItemSettings ItemData = new ItemSettings();
        public readonly Dictionary<string, ItemSettings> SceneItemData = new Dictionary<string, ItemSettings>();
        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();
        public override string GetVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            string ver = Version.ToString("0.000");

            using SHA1 sha1 = SHA1.Create();
            using FileStream stream = File.OpenRead(asm.Location);

            byte[] hashBytes = sha1.ComputeHash(stream);

            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return $"{ver}-{hash.Substring(0, 6)}";
        }
        public KeyCode ToggleEdit => Settings.ToggleEditKey;
        public KeyCode SwitchGroup => Settings.SwitchGroupKey;

        public const float Version = 0.50f;
    }
    public static class Logger
    {
        public static void Log(object obj) => DecorationMaster.instance.Log(obj);
        public static void LogDebug(object obj) => DecorationMaster.instance.LogDebug(obj);
        public static void LogWarn(object obj) => DecorationMaster.instance.LogWarn(obj);
        public static void LogError(object obj) => DecorationMaster.instance.LogError(obj);
    }
}
