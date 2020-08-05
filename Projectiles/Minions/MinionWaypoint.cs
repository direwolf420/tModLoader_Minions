﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoMod.Projectiles.Minions
{
    class MinionWaypoint : ModProjectile
    {
        public const int duration = 180000; // a long time
        public override void SetDefaults()
        {
            base.SetDefaults();
            projectile.damage = 0;
            projectile.width = 1;
            projectile.height = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = duration;
            projectile.friendly = false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            return false;
        }

        public override void AI()
        {
            if(projectile.timeLeft % 6 != 0)
            {
                return;
            }
            for(int i = 0; i < 2; i++)
            {
                Dust.NewDust(projectile.Center, 8, 8, DustID.Shadowflame);
            }
            if(Vector2.Distance(projectile.position, Main.player[Main.myPlayer].position) > 2000)
            {
                projectile.Kill();
            }
        }

        // doesn't matter, never drawn
        public override string Texture => "Terraria/NPC_0"; 
    }
}