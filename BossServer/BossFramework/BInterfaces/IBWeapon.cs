using BossFramework.BModels;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BossFramework.BInterfaces
{
    public interface IBWeapon
    {
        /// <summary>
        /// 武器名称, 也许会有用
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 武器物品ID
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// 武器前缀, 用于区分自定义武器的标识符
        /// </summary>
        public int Prefix { get; }

        public int Width { get; }
        public int Height { get; }
        public Color Color { get; }
        public int KnockBack { get; }
        public int AnimationTime { get; }
        public int UseTime { get; }
        public int ShootProj { get; }
        public int ShootSpeed { get; }
        public int Size { get; }
        public int Ammo { get; }
        public int UseAmmo { get; }
        public bool NoAmmo { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool OnAttack(BPlayer from, BPlayer target);

        public bool OnProjHit()
        {
            return false;
        }
    }
}
