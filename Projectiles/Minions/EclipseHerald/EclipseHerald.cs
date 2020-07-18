﻿using DemoMod.Projectiles.Minions.MinonBaseClasses;
using log4net.Repository.Hierarchy;
using log4net.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace DemoMod.Projectiles.Minions.EclipseHerald
{
    public class EclipseHeraldMinionBuff: MinionBuff
    {
        public EclipseHeraldMinionBuff() : base(ProjectileType<EclipseHeraldMinion>(), ProjectileType<EclipseHeraldMinion>()) { }
        public override void SetDefaults()
        {
            base.SetDefaults();
			DisplayName.SetDefault("Eclipse Herald");
			Description.SetDefault("A possessed dagger will fight for you!");
        }
    }

    public class EclipseHeraldMinionItem: EmpoweredMinionItem<EclipseHeraldMinionBuff, EclipseHeraldMinion>
    {
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Eclipse Herald Staff");
			Tooltip.SetDefault("Summons a possessed dagger to fight for you!");
		}

		public override void SetDefaults() {
			base.SetDefaults();
			item.knockBack = 0.5f;
			item.mana = 10;
			item.width = 32;
			item.height = 32;
			item.value = Item.buyPrice(0, 30, 0, 0);
			item.rare = ItemRarityID.Blue;
		}
    }


    public class EclipseMinion : SimpleMinion<EclipseHeraldMinionBuff>
    {
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Eclipse");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[projectile.type] = 6;
		}

		public sealed override void SetDefaults() {
			base.SetDefaults();
			projectile.width = 64;
			projectile.height = 64;
            projectile.minionSlots = 0;
			projectile.tileCollide = false;
            projectile.type = ProjectileType<EclipseMinion>();
		}

        public override Vector2? FindTarget()
        {
            return null;
        }

        public override Vector2 IdleBehavior()
        {
            // find parent EclipseHerald
            Projectile parent = GetMinionsOfType(ProjectileType<EclipseHeraldMinion>()).FirstOrDefault();
            if(parent == default)
            {
                Main.NewText("I can't find my mommy!");
                return Vector2.Zero;
            }
            // want to hover above and behind herald
            Vector2 target = parent.Top;
            target.Y -= 48;
            target.X -= 25;
            target.X += 5 * parent.rotation; // tilt as parent moves
            return target - projectile.position;
        }

        public override void IdleMovement(Vector2 vectorToIdlePosition)
        {
            projectile.position += vectorToIdlePosition;
            projectile.velocity = Vector2.Zero;
            projectile.rotation += (float)Math.PI / 90;
        }

        public override void TargetedMovement(Vector2 vectorToTargetPosition)
        {
            // no op
        }

        public override void Animate(int minFrame = 0, int? maxFrame = null)
        {
            projectile.frame = Math.Min(6, (int)projectile.ai[0]);
        }
    }
    public class EclipseHeraldMinion : EmpoweredMinion<EclipseHeraldMinionBuff>
    {

        private int framesSinceLastHit;
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Eclipse Herald");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[projectile.type] = 8;
		}

		public sealed override void SetDefaults() {
			base.SetDefaults();
			projectile.width = 46;
			projectile.height = 50;
			projectile.tileCollide = false;
            projectile.type = ProjectileType<EclipseHeraldMinion>();
            projectile.ai[0] = 0;
            framesSinceLastHit = 0;
            projectile.friendly = true;
            frameSpeed = 10;
		}

        public override Vector2 IdleBehavior()
        {
            base.IdleBehavior();
            Vector2 idlePosition = player.Top;
            idlePosition.X += 48 * -player.direction;
            idlePosition.Y += -32;
            Vector2 vectorToIdlePosition = idlePosition - projectile.Center;
            TeleportToPlayer(vectorToIdlePosition, 2000f);
            return vectorToIdlePosition;
        }

        public override void TargetedMovement(Vector2 vectorToTargetPosition)
        {
            // stay floating behind the player at all times
            IdleMovement(vectorToIdle);
            if(player.ownedProjectileCounts[ProjectileType<EclipseMinion>()] == 0)
            {
                Projectile.NewProjectile(projectile.position, Vector2.Zero, ProjectileType<EclipseMinion>(), 0, 0, Main.myPlayer, projectile.minionSlots - 1);
            }
            framesSinceLastHit++;
            if(framesSinceLastHit ++ > 60)
            {
                Projectile.NewProjectile(projectile.position + vectorToTargetPosition, Vector2.Zero, 
                    ProjectileType<ExampleLaser>(), 
                    projectile.damage, 
                    projectile.knockBack, 
                    Main.myPlayer);
                framesSinceLastHit = 0;
            }
            Lighting.AddLight(projectile.position, Color.White.ToVector3() * 0.5f);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            base.OnHitNPC(target, damage, knockback, crit);
            framesSinceLastHit = 0;
        }
        protected override int ComputeDamage()
        {
            return 30 + 20 * (int)projectile.minionSlots;
        }

        public override Vector2? FindTarget()
        {
            Vector2? target = base.FindTarget();
            if(target == null && player.ownedProjectileCounts[ProjectileType<EclipseMinion>()] > 0)
            {
                Projectile child = GetMinionsOfType(ProjectileType<EclipseMinion>()).FirstOrDefault();
                for(int i = 0; i < 5; i++)
                {
                    Dust.NewDust(child.Center, child.width, child.height, DustID.Shadowflame);
                }
                child.Kill(); // classic line
            }
            return target;
        }

        protected override void OnEmpower()
        {
            base.OnEmpower();
            Projectile child = GetMinionsOfType(ProjectileType<EclipseMinion>()).FirstOrDefault();
            if(child != default)
            {
                child.ai[0] = projectile.minionSlots - 1;
            }
        }

        protected override float ComputeSearchDistance()
        {
            return 700 + 50 * projectile.minionSlots;
        }

        protected override float ComputeInertia()
        {
            return 5;
        }

        protected override float ComputeTargetedSpeed()
        {
            return 16;
        }

        protected override float ComputeIdleSpeed()
        {
            return 16;
        }

        protected override void SetMinAndMaxFrames(ref int minFrame, ref int maxFrame)
        {
            if(vectorToTarget == null)
            {
                minFrame = 0;
                maxFrame = 7;
            } else
            {
                minFrame = 7;
                maxFrame = 8;
            }
        }

        public override void Animate(int minFrame = 0, int? maxFrame = null)
        {
            base.Animate(minFrame, maxFrame);

            if(Math.Abs(projectile.velocity.X) > 2)
            {
                projectile.spriteDirection = projectile.velocity.X > 0 ? -1 : 1;
            }
            projectile.rotation = projectile.velocity.X * 0.05f;
        }
    }
}