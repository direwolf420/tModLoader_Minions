using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace DemoMod.Projectiles.Minions.FlockStaff
{
	/*
	 * This file contains all the code necessary for a minion
	 * - ModItem
	 *     the weapon which you use to summon the minion with
	 * - ModBuff
	 *     the icon you can click on to despawn the minion
	 * - ModProjectile 
	 *     the minion itself
	 *     
	 * It is not recommended to put all these classes in the same file. For demonstrations sake they are all compacted together so you get a better overwiew.
	 * To get a better understanding of how everything works together, and how to code minion AI, read the guide: https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide
	 * This is NOT an in-depth guide to advanced minion AI
	 */

	public class FlockMinionBuff : MinionBuff
	{
		public FlockMinionBuff() : base(
			ProjectileType<FlockMinion>(), 
			ProjectileType<FlockMinion2>(), 
			ProjectileType<FlockMinion3>()) {}

        public override void SetDefaults()
        {
            base.SetDefaults();
			DisplayName.SetDefault("Staff of Flocking");
			Description.SetDefault("A flock of birds will fight for you!");
        }
    }

	public class FlockMinionItem : MinionItem<FlockMinionBuff, FlockMinion>
	{
		private int numShots = 0;
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Staff of Flocking");
			Tooltip.SetDefault("Summons a flock of birds to fight for you!");
		}

		public override void SetDefaults() {
			base.SetDefaults();
			item.damage = 8;
			item.knockBack = 2f;
			item.mana = 10;
			item.width = 32;
			item.height = 32;
			item.value = Item.buyPrice(0, 30, 0, 0);
			item.rare = ItemRarityID.Blue;
		}

		public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
			// This is needed so the buff that keeps your minion alive and allows you to despawn it properly applies
			player.AddBuff(item.buffType, 2);

			// Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position.
			position = Main.MouseWorld;
			int minionType;
			switch(numShots++ % 3)
            {
				case 0:
					minionType = ProjectileType<FlockMinion>();
                    break;
				case 1:
					minionType = ProjectileType<FlockMinion2>();
                    break;
				default:
					minionType = ProjectileType<FlockMinion3>();
                    break;
            }
			Projectile.NewProjectile(position, Vector2.Zero, minionType, item.damage, item.knockBack, Main.myPlayer);
			return false;
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.DirtBlock, 5);
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}

	/*
	 * This minion shows a few mandatory things that make it behave properly. 
	 * Its attack pattern is simple: If an enemy is in range of 43 tiles, it will fly to it and deal contact damage
	 * If the player targets a certain NPC with right-click, it will fly through tiles to it
	 * If it isn't attacking, it will float near the player with minimal movement
	 */
	public class FlockMinion : SimpleMinion<FlockMinionBuff>
	{
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			DisplayName.SetDefault("Flocking Bird");
			// Sets the amount of frames this minion has on its spritesheet
			Main.projFrames[projectile.type] = 4;
		}

		public sealed override void SetDefaults() {
			base.SetDefaults();
			projectile.width = 30;
			projectile.height = 24;
		}

        public override Vector2 IdleBehavior()
        {
			Vector2 idlePosition = player.Center;
			idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

			// If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
			// The index is projectile.minionPos
			float minionPositionOffsetX = (10 + projectile.minionPos * 40) * -player.direction;
			idlePosition.X += minionPositionOffsetX; // Go behind the player
			Vector2 vectorToIdlePosition = idlePosition - projectile.Center;

			// All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

			// Teleport to player if distance is too big
			TeleportToPlayer(vectorToIdlePosition, 2000f);

			// If your minion is flying, you want to do this independently of any conditions
			float overlapVelocity = 0.04f;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				// Fix overlap with other minions
				Projectile other = Main.projectile[i];
				if (i != projectile.whoAmI && other.active && other.owner == projectile.owner && Math.Abs(projectile.position.X - other.position.X) + Math.Abs(projectile.position.Y - other.position.Y) < projectile.width) {
					if (projectile.position.X < other.position.X) projectile.velocity.X -= overlapVelocity;
					else projectile.velocity.X += overlapVelocity;

					if (projectile.position.Y < other.position.Y) projectile.velocity.Y -= overlapVelocity;
					else projectile.velocity.Y += overlapVelocity;
				}
			}
			return vectorToIdlePosition;
        }

        public override Vector2? FindTarget()
        {
			projectile.friendly = true;
            if (PlayerTargetPosition(2000f) is Vector2 target)
            {
                return target - projectile.Center;
            }
            else if (ClosestEnemyInRange(700f) is Vector2 target2)
            {
                return target2 - projectile.Center;
            }
            else
            {
				projectile.friendly = false;
                return null;
            }
        }

        public override void TargetedMovement(Vector2 vectorToTargetPosition)
        {
			// Default movement parameters (here for attacking)
			float speed = 8f;
			float inertia = 20f;
            // Minion has a target: attack (here, fly towards the enemy)
            if (vectorToTargetPosition.Length() > 40f) {
                // The immediate range around the target (so it doesn't latch onto it when close)
                vectorToTargetPosition.Normalize();
                vectorToTargetPosition *= speed;
                projectile.velocity = (projectile.velocity * (inertia - 1) + vectorToTargetPosition) / inertia;
            }
        }

        public override void IdleMovement(Vector2 vectorToIdlePosition)
        {
			float speed;
			float inertia;
            // Minion doesn't have a target: return to player and idle
            if (vectorToIdlePosition.Length() > 600f) {
                // Speed up the minion if it's away from the player
                speed = 12f;
                inertia = 60f;
            }
            else {
                // Slow down the minion if closer to the player
                speed = 4f;
                inertia = 80f;
            }
            if (vectorToIdlePosition.Length() > 20f) {
                // The immediate range around the player (when it passively floats about)

                // This is a simple movement formula using the two parameters and its desired direction to create a "homing" movement
                vectorToIdlePosition.Normalize();
                vectorToIdlePosition *= speed;
                projectile.velocity = (projectile.velocity * (inertia - 1) + vectorToIdlePosition) / inertia;
            }
            else if (projectile.velocity == Vector2.Zero) {
                // If there is a case where it's not moving at all, give it a little "poke"
                projectile.velocity.X = -0.15f;
                projectile.velocity.Y = -0.05f;
            }
        }

        public override void Animate(int minRange = 0, int? maxRange = null)
        {
			base.Animate();
			// So it will lean slightly towards the direction it's moving
			projectile.rotation = projectile.velocity.X * 0.05f;
			projectile.spriteDirection = projectile.velocity.X > 0 ? -1: 1;

			// Some visuals here
			//Lighting.AddLight(projectile.Center, Color.White.ToVector3() * 0.78f);
        }
	}

	public class FlockMinion2: FlockMinion
    {

    }

	public class FlockMinion3: FlockMinion
    {

    }
}