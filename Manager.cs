﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;
using DecorationMaster.UI;
using DecorationMaster.MyBehaviour;
using DecorationMaster.Attr;
using System.Collections;

namespace DecorationMaster
{
    public class ItemManager
    {
        public delegate void SelectChanged(CustomDecoration d);
        public event SelectChanged OnChanged;
        private static ItemManager _instance;
        private GameObject _setup_flag_backing;
        public GameObject currentSelect { get; private set; }
        public delegate void GroupSwitchHandler(string[] nextGroup);
        public event GroupSwitchHandler GroupSwitchEventHandler;
        private GameObject SetupFlag { get
            {
                if (_setup_flag_backing != null)
                    return _setup_flag_backing;
                SetupFlagInit();
                return _setup_flag_backing;
            }
            set
            {
                _setup_flag_backing = value;
            }
        }
        public static Dictionary<int, string[]> group = new Dictionary<int, string[]>();
        public const int GroupMax = 6;
        public int CurrentGroup { get; private set; } = 1;
        public bool setupMode { get; private set; }

        public CircleStack<GameObject[]> AddedHistory = new CircleStack<GameObject[]>(DecorationMaster.instance.Settings.HistroyMaxCount);
        public static ItemManager Instance {
            get
            {
                if (_instance == null)
                    _instance = new ItemManager();
                return _instance;

            }
        }
        private ItemManager()
        {
            if (SetupFlag == null)
                SetupFlagInit();
            IEnumerable<KeyValuePair<string, GameObject>> deGo;
            if (DecorationMaster.instance.Settings.ProfessorMode)
            {
                //deGo = ObjectLoader.InstantiableObjects.Where(x => x.Value.GetComponent<CustomDecoration>() != null);
                deGo = ObjectLoader.InstantiableObjects.Where(x =>
                {
                    var cd = x.Value.GetComponent<CustomDecoration>();
                    return (
                    (cd != null)
                    && (!cd.GetType().IsDefined(typeof(ObsoleteAttribute), false))
                    );
                });
            }
            else if (DecorationMaster.instance.Settings.MemeItem)
            {
                deGo = ObjectLoader.InstantiableObjects.Where(x =>
                {
                    var cd = x.Value.GetComponent<CustomDecoration>();
                    return (
                    (cd != null)
                    && (!cd.GetType().IsDefined(typeof(AdvanceDecoration), false))
                    );
                });
            }
            else
            {
                deGo = ObjectLoader.InstantiableObjects.Where(x =>
                {
                    var cd = x.Value.GetComponent<CustomDecoration>();
                    return (
                    (cd != null)
                    && (!cd.GetType().IsDefined(typeof(AdvanceDecoration), false))
                    && (!cd.GetType().IsDefined(typeof(MemeDecoration), false))
                    );
                });
            }

            var Names = deGo.Select(x => x.Key);
            int group_idx = 0;

            while (Names.Any())
            {
                group_idx++;
                var a = Names.Take(GroupMax).ToArray();
                Names = Names.Skip(GroupMax);
                group.Add(group_idx, a);
            }

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }
        ~ItemManager()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }
        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            ClearHistory();
        }

        public int SwitchGroup(int span = 1)
        {
            var next_group = CurrentGroup + span;
            if (!group.ContainsKey(next_group))
                next_group = 1;
            CurrentGroup = next_group;
            SetupFlag.GetComponent<TextMeshPro>().text = $"Editing [{CurrentGroup}]";

            GroupSwitchEventHandler?.Invoke(group[CurrentGroup]);

            return CurrentGroup;
        }
        public bool ToggleSetup()
        {
            if (HeroController.instance == null)
            {
                setupMode = false;
                return setupMode;
            }
            setupMode = !setupMode;
            if (setupMode)
            {
                SetupFlag.SetActive(true);
                SetupFlag.transform.SetParent(HeroController.instance.transform);
                SetupFlag.transform.localPosition = new Vector3(0, 1.5f);
                GameManager.instance.inputHandler.StartUIInput();
            }
            else
            {
                SetupFlag.SetActive(false);
            }
            PickPanel.SetActive(setupMode);
            return setupMode;
        }
        public CustomDecoration Select(int idx)
        {
            if (!setupMode)
                return null;

            if (currentSelect != null)
                return currentSelect.GetComponent<CustomDecoration>();

            if (idx < 1 || idx > group[CurrentGroup].Length)
                return null;

            string poolname = group[CurrentGroup][idx - 1];
            GameObject go = ObjectLoader.CloneDecoration(poolname);
            //currentSelect = go;
            //CustomDecoration cd = go?.GetComponent<CustomDecoration>();
            //go?.SetActive(true);

            //OnChanged?.Invoke(cd);
            var cd = _select_go(go);
            return cd;
        }
        public CustomDecoration Select(Item i)
        {
            if (!setupMode)
                return null;

            if (currentSelect != null)
                RemoveCurrent();

            GameObject go = ObjectLoader.CloneDecoration(i);
            var cd = _select_go(go);
            return cd;
        }

        private CustomDecoration _select_go(GameObject go)
        {
            currentSelect = go;
            var cd = go?.GetComponent<CustomDecoration>();
            go?.SetActive(true);
            OnChanged?.Invoke(cd);
            return cd;
        }

        [Obsolete("Do not use")]
        public CustomDecoration Select(GameObject prefab)
        {
            if (prefab == null)
                return null;
            if (currentSelect != null)
                return currentSelect.GetComponent<CustomDecoration>();
            var clone = prefab.GetComponent<CustomDecoration>().Setup(Operation.COPY, null) as GameObject;
            /*currentSelect = clone;
            currentSelect.SetActive(true);
            var cd = clone.GetComponent<CustomDecoration>();
            OnChanged?.Invoke(cd);*/
            var cd = _select_go(clone);
            return cd;
        }
        internal void orig_Operate(Operation op,object val)
        {
            if (currentSelect == null)
                return;
            var d = currentSelect.GetComponent<CustomDecoration>();
            d.Setup(op, val);
        }
        public void Operate(Operation op, object val)
        {
            orig_Operate(op, val);
        }
        public void AddCurrent()
        {
            if (currentSelect == null)
                return;
            Operate(Operation.ADD, null);
            RecordHistory(new GameObject[] { currentSelect });
            currentSelect = null;
        }
        public void AddBlock(IEnumerable<GameObject> block)
        {
            if (block == null)
                return;
            var array = block.ToArray();
            for (int i = 0; i < array.Length; i++)
                array[i].GetComponent<CustomDecoration>().Setup(Operation.ADD, null);
            if(array.Length>0)
                RecordHistory(array);
        }
        public void RecordHistory(GameObject[] h)
        {
            AddedHistory.Push(h);
            Logger.LogDebug("Add Histroy");
        }
        public void DiscardLast()
        {
            if (!setupMode)
                return;
            //Logger.LogDebug("Try Discarding");
            var last = AddedHistory.Pop();
            if(last!=null && last.Length>0)
            {
                for (int i = 0; i < last.Length; i++)
                    if(last[i] != null)
                        last[i].GetComponent<CustomDecoration>().Remove();

                //Logger.LogDebug("Discard last added");
            }
        }
        public void CopyBlock()
        {
            if (!setupMode)
                return;

            Block.Instance.Select(Block.BlockOp.COPY);
        }
        public void MoveBlock()
        {
            if (!setupMode)
                return;

            Block.Instance.Select(Block.BlockOp.MOVE);
        }
        public void DelBlock()
        {
            if (!setupMode)
                return;

            Block.Instance.Select(Block.BlockOp.DELETE);
        }

        public void ClearHistory()
        {
            AddedHistory.Clear();
        }
        public void RemoveCurrent()
        {
            if (currentSelect == null)
                return;
            currentSelect.SetActive(false);
            Object.DestroyImmediate(currentSelect);
            currentSelect = null;
        }
        private void SetupFlagInit()
        {
            if (_setup_flag_backing != null)
                Object.DestroyImmediate(_setup_flag_backing);
            SetupFlag = new GameObject("text");
            var text = SetupFlag.AddComponent<TextMeshPro>();
            text.text = $"Editing:[{CurrentGroup}]";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.1f;
            SetupFlag.AddComponent<KeepWorldScalePositive>();
            //SetupFlag.AddComponent<HookCursor>();
            SetupFlag.AddComponent<MyCursor>();
            SetupFlag.transform.SetScaleX(0.2f);
            SetupFlag.transform.SetScaleY(0.2f);
            SetupFlag.SetActive(false);
            Object.DontDestroyOnLoad(SetupFlag);

        }
        /*private class HookCursor : MonoBehaviour
        {
            private void OnEnable()
            {
                On.InputHandler.OnGUI += InputHandler_OnGUI;
                var m = GameManager.instance.inputHandler.GetType().GetMethod("SetCursorVisible", BindingFlags.NonPublic | BindingFlags.Instance);
                m.Invoke(GameManager.instance.inputHandler, new object[] { true, });
            }

            private void InputHandler_OnGUI(On.InputHandler.orig_OnGUI orig, InputHandler self) { }

            private void OnDisable()
            {
                On.InputHandler.OnGUI -= InputHandler_OnGUI;
            }
        }
        */
    }
    public class MyCursor : MonoBehaviour
    {
        //private static GameObject arrow;
        private static bool draw = false;
        public static Texture2D cursorTexture;
        public static Vector3 CursorPosition;
        private void Awake()
        {
            cursorTexture = GUIController.Instance.images["arrow"];
        }
        private void OnGUI()
        {
            if (!draw)
                return;
            var mousePos = Input.mousePosition;
            GUI.DrawTexture(new Rect(mousePos.x, Screen.height - mousePos.y, cursorTexture.width, cursorTexture.height), cursorTexture);
            var screenPos = Camera.main.WorldToScreenPoint(new Vector3(0, 0, 0));
            mousePos.z = screenPos.z;
            CursorPosition = Camera.main.ScreenToWorldPoint(mousePos);
        }
        
        private void OnEnable()
        {
            draw = true;
        }
        private void OnDisable()
        {
            draw = false;
        }

    }

    public class CircleStack<T>
    {
        //int max_count { get => array.Length; }
        private int idx;
        private T[] array;
        public CircleStack(int max_capacity)
        {
            idx = 0;
            //max_count = max_capacity;
            array = new T[max_capacity];
        }
        public void Push(T item)
        {
            array[idx] = item;
            idx = (idx + 1) % array.Length;
        }
        public T Pop()
        {
            if (idx == 0)
                idx = array.Length - 1;
            else
                idx = (idx - 1) % array.Length;

            T item = array[idx];
            array[idx] = default;

            return item;
        }
        public T Peek()
        {
            if (idx == 0)
                idx = array.Length - 1;
            else
                idx = (idx - 1) % array.Length;
            return array[idx];
        }
    
        public void Clear()
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = default;
            idx = 0;
        }
    }

}
