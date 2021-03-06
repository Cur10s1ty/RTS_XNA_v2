﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using PathfindingTest.Pathfinding;
using PathfindingTest.Players;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PathfindingTest.Combat;
using PathfindingTest.Multiplayer.Data;
using PathfindingTest.Buildings;

namespace PathfindingTest.Units.Projectiles
{
    public abstract class Projectile : DamageSource
    {
        private float startX { get; set; }
        private float startY { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public Texture2D texture { get; set; }

        private Boolean hasToMove { get; set; }
        public float movementSpeed { get; set; }
        public float direction { get; set; }
        public int maxRange { get; set; }

        protected Point waypoint { get; set; }

        public Bowman parent { get; set; }
        public ProjectileMultiplayerData multiplayerData { get; set; }


        public Projectile(Unit parent, Damageable target, DamageEvent.DamageType type, float movementSpeed, int maxRange, int baseDamage)
        {
            this.parent = (Bowman)parent;
            this.x = parent.x;
            this.y = parent.y;
            this.startX = this.x;
            this.startY = this.y;

            this.target = target;
            this.type = type;
            this.movementSpeed = movementSpeed;
            this.maxRange = maxRange;
            this.baseDamage = baseDamage;

            //this.waypoint = Util.GetPointOnCircle(parent.GetLocation(), 
            //    (int) Util.GetHypoteneuseLength(this.GetLocation(), target.GetLocation()),
            //    Util.GetHypoteneuseAngleDegrees(parent.GetLocation(), target.GetLocation()));
            if (target is Unit)
            {
                int targetX = (int)((Unit)target).x;
                int targetY = (int)((Unit)target).y;
                this.waypoint = new Point(targetX, targetY);
                SetMoveToTarget(targetX, targetY);
            }
            else
            {
                int targetX = (int)((Building)target).x;
                int targetY = (int)((Building)target).y;
                this.waypoint = new Point(targetX, targetY);
                SetMoveToTarget(targetX, targetY);
            }

            if (Game1.GetInstance().IsMultiplayerGame())
            {
                Boolean isLocal = this.parent.player == Game1.CURRENT_PLAYER;
                this.multiplayerData = new ProjectileMultiplayerData(this, isLocal);
                if (isLocal)
                {
                    this.multiplayerData.RequestServerID();
                }
            }
        }

        /// <summary>
        /// Updates this projectile.
        /// </summary>
        public void Update(KeyboardState ks, MouseState ms)
        {
            UpdateMovement();
        }


        /// <summary>
        /// Updates the movement of this unit.
        /// </summary>
        protected void UpdateMovement()
        {
            // Point target = this.waypoints.ElementAt(0);
            Move();
        }

        /// <summary>
        /// Set the point this Engineer has to move to.
        /// direction != direction is used for checking NaNExceptions.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void SetMoveToTarget(int x, int y)
        {
            double a = Math.Abs(this.x - x);
            double b = Math.Abs(this.y - y);
            direction = (float)Math.Atan(a / b);
            if (direction != direction)
            {
                hasToMove = false;
                return;
            }
            hasToMove = true;
        }

        /// <summary>
        /// Updates the drawing position of this Engineer.
        /// </summary>
        private void Move()
        {
            if (!hasToMove) return;
            // lkllllllllthis.SetMoveToTarget((int)this.target.x, (int)this.target.y);

            float xSpeedDirection = movementSpeed * (float)Math.Sin(direction);
            float ySpeedDirection = movementSpeed * (float)Math.Cos(direction);

            if (x < waypoint.X && y < waypoint.Y)
            {
                x += xSpeedDirection;
                y += ySpeedDirection;
            }
            else if (x < waypoint.X && y > waypoint.Y)
            {
                x += xSpeedDirection;
                y -= ySpeedDirection;
            }
            else if (x < waypoint.X && y == waypoint.Y)
            {
                x += xSpeedDirection;
            }
            else if (x > waypoint.X && y < waypoint.Y)
            {
                x -= xSpeedDirection;
                y += ySpeedDirection;
            }
            else if (x > waypoint.X && y > waypoint.Y)
            {
                x -= xSpeedDirection;
                y -= ySpeedDirection;
            }
            else if (x > waypoint.X && y == waypoint.Y)
            {
                x -= xSpeedDirection;
            }
            else if (x == waypoint.X && y < waypoint.Y)
            {
                y += ySpeedDirection;
            }
            else if (x == waypoint.X && y > waypoint.Y)
            {
                y -= ySpeedDirection;
            }

            // Commented = code that makes the arrow die on impact with collision
            if (/*Game1.GetInstance().collision.CollisionAt(
                Util.GetPointOnCircle(this.GetLocation(), this.texture.Height / 2,
                        (float)(Util.GetHypoteneuseAngleDegrees(this.GetLocation(), this.waypoint)))) ||*/
                Math.Abs(x - waypoint.X) < movementSpeed && Math.Abs(y - waypoint.Y) < movementSpeed)
            {
                //Console.Out.WriteLine("Projectile gotten to waypoint.");
                if (target is Unit && !((Unit)target).isDead)
                {
                    this.waypoint = new Point((int)((Unit)target).x, (int)((Unit)target).y);
                    SetMoveToTarget((int)((Unit)target).x, (int)((Unit)target).y);
                }
                else if (target is Building && !((Building)target).isDestroyed)
                {
                    this.waypoint = new Point((int)((Building)target).x, (int)((Building)target).y);
                    SetMoveToTarget((int)((Building)target).x, (int)((Building)target).y);
                }
                else
                {
                    Console.WriteLine("target of unknown type, or dead");
                    this.Dispose();
                }
            }
            else
            {
                CheckCollision();
            }
        }

        private void CheckCollision()
        {
            // Collision events are handled by the owning player, including arrow remove events
            if (Game1.GetInstance().IsMultiplayerGame() &&
                (this.parent.player != Game1.CURRENT_PLAYER)) return;

            Point collisionLocation = Util.GetPointOnCircle(this.GetLocation(), this.texture.Height / 2,
                        (float)(Util.GetHypoteneuseAngleDegrees(this.GetLocation(), this.waypoint)));
            foreach (Player player in Game1.GetInstance().players)
            {
                if (player.alliance.members.Contains(parent.player)) continue;
                else
                {
                    if (target is Unit)
                    {
                        foreach (Unit unit in player.units)
                        {
                            // Check if the units are close enough
                            if (unit.DefineRectangle().Contains(
                                // Front of projectile!
                                collisionLocation))
                            {
                                Hit();
                            }
                        }
                    }
                    else if (target is Building)
                    {
                        foreach (Building building in player.buildings)
                        {
                            if (building.DefineRectangle().Contains(
                                // Front of projectile!
                                collisionLocation))
                            {
                                Hit();
                            }
                        }
                    }

                }
            }
        }

        internal abstract void Draw(SpriteBatch sb);

        public Point GetLocation()
        {
            return new Point((int)this.x, (int)this.y);
        }

        public void Hit()
        {
            Console.WriteLine("Target has been hit!");
            DamageEvent e = new DamageEvent(this, target, parent);
            target.OnDamage(e);
            if (Game1.GetInstance().IsMultiplayerGame())
            {
                Synchronizer.GetInstance().QueueDamageEvent(e);
            }
            else
            {
                this.Dispose();
            }
            return;
        }

        /// <summary>
        /// Disposes of this unit
        /// </summary>
        public void Dispose()
        {
            this.parent.projectiles.Remove(this);
            this.x = -20;
            this.y = -20;
            this.maxRange = 1;

            Console.Out.WriteLine("Disposing projectile");
        }
    }
}
