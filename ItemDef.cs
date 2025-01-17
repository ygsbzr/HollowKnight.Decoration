﻿using DecorationMaster.Attr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;
using DecorationMaster.UI;
using DecorationMaster.MyBehaviour;
using DecorationMaster.Util;

namespace DecorationMaster
{
    public interface IAssemable
    {
        public void AcceptSetup(Operation op, object val);
    }

    [Serializable]
    public abstract class Item : ICloneable
    {
        //public List<Item> additionItem = new List<Item>();

        [InspectIgnore]
        public string sceneName { get; set; }
        [InspectIgnore]
        public virtual string pname { get; set; }

        [InspectIgnore]
        [Handle(Operation.SetPos)]
        public V2 position { get; set; }

        /*[Description("设置物品的生成顺序，切图后生效")]
        [IntConstraint(-50, 50)]
        [Handle(Operation.SetSpawnOrder)]
        public virtual int SpawnOrder { get; set; } = 0;*/

        public void Setup(Operation op, object val)
        {
            var handle_prop = ReflectionCache.GetItemProps(GetType(), op);
            if (handle_prop == null)
                return;

            foreach (var prop in handle_prop)
            {
                try
                {
                    if(val.GetType()==typeof(Vector2))
                    {
                        prop.SetValue(this, new V2((Vector2)val),null);
                    }
                    else
                    {
                        prop.SetValue(this, val, null);
                    }
                }
                catch (ArgumentException e)
                {
                    Logger.LogDebug($" ### Val Type:{val.GetType()},Prop Type:{prop.PropertyType}");
                    throw e;
                }
            }

            //#region trigger addition item setup
            //foreach(var i in additionItem)
            //{
            //    var m = i.GetType().GetInterface(nameof(IAssemable))?.GetMethod("AcceptSetup");
            //    m?.Invoke(i, new object[] {op, val });
            //}
            //#endregion
        }

        public object Clone()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(stream);
            }
        }
    }
    [Serializable]
    public abstract class ResizableItem : Item
    {
        [Description("设置物品的整体大小")]
        [Handle(Operation.SetSize)]
        [FloatConstraint(0.2f,2f)]
        public float size { get; set; } = 1;

        [Description("设置物品的旋转角度")]
        [Handle(Operation.SetRot)]
        [IntConstraint(0,360)]
        public int angle { get; set; } = 0;

    }

    [Serializable]
    public abstract class ColorItem : Item
    {
        [FloatConstraint(0, 1)]
        [Handle(Operation.SetColorR)]
        public float R { get; set; } = 1f;

        [FloatConstraint(0, 1)]
        [Handle(Operation.SetColorG)]
        public float G { get; set; } = 1f;

        [FloatConstraint(0, 1)]
        [Handle(Operation.SetColorB)]
        public float B { get; set; } = 1f;

        [Description("设置贴图的透明度")]
        [FloatConstraint(0, 1)]
        [Handle(Operation.SetColorA)]
        public virtual float A { get; set; } = 1f;

        public Color GetColor()
        {
            return new Color(R, G, B, A);
        }
    }

    [Serializable]
    public abstract class ManaItem : Item
    {
        [Description("设置魔力属性，总共有5种有色属性")]
        [Handle(Operation.SetMana)]
        public virtual ManaType mType { get; set; }
    }
    [Serializable]
    public class DelayItem : Item,IAssemable
    {
        [Handle(Operation.SetTimeOffset)]
        [FloatConstraint(0f, 2f)]
        public float time_offset { get; set; } = 0;

        public void AcceptSetup(Operation op, object val)
        {
            throw new NotImplementedException();
        }
    }

    public class ItemDef
    {
        [Serializable]
        [Decoration("default")]
        public class DefaultItem : Item { }

        [Serializable]
        [Decoration("HK_break_wall")]
        [Decoration("HK_unbreak_wall")]
        [Decoration("IMG_MothwingCloak")]
        [Decoration("IMG_MonarchWings")]
        [Decoration("IMG_MantisClaw")]
        [Decoration("IMG_Lantern")]
        [Decoration("IMG_DownSlash")]
        [Decoration("HK_turret")]
        [Decoration("zote_wall")]
        [Decoration("edge")]
        [Decoration("white_spike")]
        [Decoration("white_thorn")]
        [Decoration("HK_shadow_gate")]
        [Decoration("hazard_saver")]
        [Decoration("jarcol_floor")]
        [Decoration("stomper_switch")]

        public class DefatulResizeItem : ResizableItem { }

        [Serializable]
        public class PartResizeItem : Item
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
        }


        

        [Serializable]
        public class BindingItem : ResizableItem { }

        [Serializable]
        [Decoration("HK_saw")]
        [Decoration("move_flip_platform")]
        [Decoration("mary_move_platform")]
        public class SawItem : ResizableItem
        {
            [Handle(Operation.SetSpan)]
            [IntConstraint(0,15)]
            public int span { get; set; } = 3;

            [Description("设置物品的运动速度")]
            [Handle(Operation.SetSpeed)]
            [IntConstraint(-20,20)]
            public int speed { get; set; }

            //[Handle(Operation.SetTinkVoice)]
            //[FloatConstraint(0.1f,1)]
            [InspectIgnore]
            public float pitch { get; set; } = 1;

            [Description("设置音量")]
            [Handle(Operation.SetVolume)]
            [FloatConstraint(0, 1)]
            public float volume { get; set; }

            [Handle(Operation.SetPos)]
            [InspectIgnore]
            public V2 Center { get; set; }

            [Description("设置物品的起始位置偏移")]
            [Handle(Operation.SetOffset)]
            [IntConstraint(0,10)]
            public int offset { get; set; }
        }
    
    
        [Serializable]
        [Decoration("HK_lever")]
        [Decoration("HK_gate")]
        public class LeverGateItem : ResizeKeyGateItem
        {
            public const string GateNamePrefix = "CustomTollGate_";
        }

        [Serializable]
        [Decoration("Mana_Source")]
        public class ManaSourceItem : ManaItem
        {
            [Description("设置魔力属性，总共有5种有色属性")]
            [Handle(Operation.SetMana)]
            [IntConstraint((int)ManaType.W,(int)ManaType.C-1)]
            public override ManaType mType { get => base.mType; set => base.mType = value; }
        }

        [Serializable]
        [Decoration("HK_lore_tablet_1")]
        public class LoreTablet1 : ResizableItem
        {
            [Description("设置碑文的文本")]
            [StringConstraint(1,100)]
            [Handle(Operation.SetText)]
            public string Text { get; set; } = "Please Input Text Here";
        }

        [Serializable]
        public class KeyGateItem : Item
        {
            [Description("设置开关编号，对应门的编号")]
            [Handle(Operation.SetGate)]
            [IntConstraint(1,20)]
            public int Number { get; set; }
        }


        [Serializable]
        [Decoration("Mana_Wall")]
        public class ResizeKeyGateItem : ResizableItem
        {
            [Description("设置门的编号")]
            [Handle(Operation.SetGate)]
            [IntConstraint(1, 20)]
            public int Number { get; set; }
        }
        [Serializable]
        [Decoration("Mana_Requirement")]
        public class ManaRequirement : KeyGateItem
        {
            [StringConstraint(1, 16)]
            [Description("设置Mana的消耗")]
            [Handle(Operation.SetText)]
            public string Requires { get; set; } = "C";
        }

        [Serializable]
        [Decoration("twinkle_platform")]
        public class TempPlatItem : Item
        {
            [Handle(Operation.SetSize)]
            [FloatConstraint(0.2f, 2f)]
            public float size { get; set; } = 1;

            [Handle(Operation.SetRot)]
            [IntConstraint(0, 360)]
            public int angle { get; set; } = 270;

            [Description("设置闪烁周期")]
            [Handle(Operation.SetTime)]
            [IntConstraint(1, 5)]
            public int Time { get; set; } = 1;

            [Handle(Operation.SetOffset)]
            [IntConstraint(0, 1)]
            public int Offset { get; set; } = 0;
        }

        [Serializable]
        [Decoration("note_platform")]
        public class AuidoItem : Item
        {
            [IntConstraint(1, AudioBehaviours.NoteMax)]
            [Handle(Operation.SetNote)]
            public int Note { get; set; } = 1;
        }

        [Serializable]
        [Decoration("HK_Lconveyor")]
        [Decoration("HK_Rconveyor")]
        [Decoration("simple_conveyor")]
        public class ConveyorItem : ResizableItem
        {
            [Description("设置传送带的速度")]
            [Handle(Operation.SetSpeed)]
            [IntConstraint(1, 20)]
            public int speed { get; set; } = 8;
        }
    }
}
