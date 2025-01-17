﻿using DecorationMaster.Attr;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using DecorationMaster.Util;
using DecorationMaster.UI;
namespace DecorationMaster.MyBehaviour
{
    public abstract class Editable : MonoBehaviour, IHitResponder
    {
        public virtual bool SetupMode { get => ItemManager.Instance.setupMode; }
        public virtual void Hit(HitInstance damageInstance)
        {
           // Logger.LogDebug($"Hit Mode:{SetupMode}");
            if (SetupMode && ((DecorationMaster.instance.Settings.allowSpellRemove) || damageInstance.AttackType == AttackTypes.Nail))
                Remove();
        }
        public abstract void Add(object self=null); //add this item to global
        public virtual void Remove(object self=null) //remove this item from global
        {
            
            if (self == null)
                Destroy(gameObject);
            else
            {
                try
                {
                    Destroy(self as GameObject);
                }
                catch
                {
                    Logger.LogWarn("An Exception ocurr on Editable.Remove()");
                }
            }
        }
    
    }

    public abstract class CustomDecoration : Editable
    {
        public Item item;
        /// <summary>
        /// Setup the value of item, and do some effect base on item
        /// To Deal with An Operation, you must Add a Method with HandleAttribute that match OP enum
        /// </summary>
        /// <param name="op"></param>
        /// <param name="val">the type must base on Item Prop</param>
        public object Setup(Operation op, object val)
        {
            item?.Setup(op, val);

            var handlers = ReflectionCache.GetMethods(GetType(), op);
            if (handlers == null)
                return null;

            object _return = null;
            foreach (var m in handlers)
            {
                object[] args;
                if (val == null)
                {
                    ArrayList objList = new ArrayList();
                    for (int i = 0; i < m.GetParameters().Length; i++)
                        objList.Add(null);
                    args = objList.ToArray();
                    Logger.LogDebug($"argument null,fill with {args.Length} null");
                } 
                else
                    args = new object[] { val.GetType() == typeof(V2) ? ((Vector2)((V2)val)) : val, };
                object mechod_ret = m.Invoke(this, args);
                _return = mechod_ret == null ? _return: mechod_ret;
            }
            return _return;
        }
        
        [Handle(Operation.SetPos)]
        public virtual void HandlePos(Vector2 val)
        {
            gameObject.transform.position = new Vector3(val.x, val.y, gameObject.transform.position.z);
        }
        [Handle(Operation.Serialize)]
        public virtual void HandleInit(Item i)
        {
            if (item != i)
                item = i;

            if(ReflectionCache.ItemPropCache.ContainsKey(i.GetType()))
            {
                ReflectionCache.GetItemProps(i.GetType(), Operation.None);
            }
            var op_props = ReflectionCache.ItemPropCache[i.GetType()];
            foreach(var kv in op_props)
            {
                Operation op = kv.Key;
                object value = kv.Value.FirstOrDefault().GetValue(i, null);
                try
                {
                    Setup(op, value);
                }
                catch (Exception e)
                {
                    Logger.LogError($"An Exception occur while Setup:Op:{op},val:{value}");
                    throw e;
                }
            }

            gameObject.SetActive(true);

            //Logger.LogDebug($"{i.pname} Serialize");
        }

        /// <summary>
        /// Add Object's Setting To Global Settings
        /// </summary>
        [Handle(Operation.ADD)]
        public override void Add(object self = null)
        {
            bool globalitem;
            if (self == null)
                self = item;

            if (self == null)
                throw new NullReferenceException("Item Null Exception");

            string sceneName = GameManager.instance.sceneName;
            item.sceneName = sceneName;

            globalitem = self.GetType().IsDefined(typeof(GlobalItemAttribute), true);

            if (globalitem)
            {
                DecorationMaster.instance.ItemData.AddItem((Item)self);
                return;
            }

            var dict = DecorationMaster.instance.SceneItemData;
            if (dict.TryGetValue(sceneName, out var _scene_data))
            {
                _scene_data.AddItem((Item)self);
            }
            else
            {
                var _setting = new ItemSettings();
                _setting.scene_name = sceneName;
                dict.Add(sceneName,_setting );
                dict[sceneName].AddItem((Item)self);
            }
            //var settings = DecorationMaster.instance.ItemData;

            //settings.items.Add((Item)self);
        }
        [Handle(Operation.REMOVE)]
        public override void Remove(object self = null)
        {
            if (self == null)
                self = item;

            Logger.LogDebug($"{((Item)self).pname} - remove self");
            var settings = DecorationMaster.instance.ItemData;
            if (self == null)
                throw new NullReferenceException("Item Null Exception");
            string sceneName = ((Item)self).sceneName;
            var dict = DecorationMaster.instance.SceneItemData;
            settings.RemoveItem((Item)self);
            if (dict.TryGetValue(sceneName, out var _scene_data))
            {
                _scene_data.RemoveItem((Item)self);
            }

            base.Remove();
        }
        [Handle(Operation.COPY)]
        public virtual GameObject CopySelf(object self = null)
        {
            var item_clone = item.Clone() as Item;
            //var clone = Instantiate(gameObject);
            //clone.GetComponent<CustomDecoration>().item = item_clone;
            var clone = ObjectLoader.CloneDecoration(item_clone);
            return clone;
        }
    }
    public abstract class Resizeable : CustomDecoration
    {
        [Handle(Operation.SetSize)]
        public virtual void HandleSize(float size)
        {
            gameObject.transform.localScale = size * Vector3.one;
        }
        [Handle(Operation.SetRot)]
        public virtual void HandleRot(float angle)
        {
            gameObject.transform.eulerAngles = new Vector3(0, 0, angle);
        }
    }
    public abstract class PartResizable : CustomDecoration
    {
        [Handle(Operation.SetSizeX)]
        public virtual void HandleSizeX(float size)
        {
            var ori = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(size, ori.y, ori.z);

        }

        [Handle(Operation.SetSizeY)]
        public virtual void HandleSizeY(float size)
        {
            var ori = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(ori.x, size, ori.z);
        }
        [Handle(Operation.SetRot)]
        public virtual void HandleRot(float angle)
        {
            gameObject.transform.eulerAngles = new Vector3(0, 0, angle);
        }
    }
    public abstract class SawMovement : Resizeable
    {
        private void Update()
        {
            var sitem = item as ItemDef.SawItem;
            var nextPoint = Move(gameObject.transform.position);
            gameObject.transform.position = nextPoint;
        }
        public abstract Vector3 Move(Vector3 current);
        public override void HandleRot(float angle)
        {
            return;
        }
    }

    public abstract class BoolBinding : Resizeable
    {
        public virtual string BindBoolValue { get; private set; }
        private void OnEnable()
        {
            if (BindBoolValue != null)
            {
                HUD.AddBindIcon(gameObject);
                ModHooks.GetPlayerBoolHook += Bind;
            }
        }

        private bool Bind(string name, bool orig)
        {

            return name == BindBoolValue ? false : orig;
        }

        private void OnDisable()
        {
            if (BindBoolValue != null)
            { 
                ModHooks.GetPlayerBoolHook -= Bind;
            }
        }
        

    }
    
    public abstract class BreakableBoolBinding : BoolBinding
    {
        private void Awake()
        {
            gameObject.name += this.GetType().Name;
            gameObject.AddComponent<NonBouncer>();
        }
        public override void Hit(HitInstance hit)
        {
            base.Hit(hit);
            Destroy(gameObject);
        }
    }

    public abstract class IntBinding : Resizeable
    {
        public virtual string BindIntValue { get; private set; }
        private void OnEnable()
        {
            if (BindIntValue != null)
            {
                HUD.AddBindIcon(gameObject);
                ModHooks.GetPlayerIntHook += Bind;
            }
        }

        private int Bind(string name, int orig)
        {
            return (name == BindIntValue) ? 0 : orig;
        }

        private void OnDisable()
        {
            if (BindIntValue != null)
                ModHooks.GetPlayerIntHook -= Bind;
        }
    }
    public abstract class BreakableIntBinding : IntBinding
    {
        private void Awake()
        {
            gameObject.name += this.GetType().Name;
            gameObject.AddComponent<NonBouncer>();
        }
        public override void Hit(HitInstance hit)
        {
            base.Hit(hit);
            Destroy(gameObject);
        }
    }
}
