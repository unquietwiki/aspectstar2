﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace aspectstar2
{
    public class AdventureEnemy : AdventureObject
    {
        protected BestiaryEntry definition;
        protected int health;
        protected int flickerCount = 2;
        public bool ghost;
        protected int radius = 16;
        protected bool interenemycollide;
        protected bool defense;
        protected int flickerTime = 40;
        public int id = -1;

        public AdventureEnemy()
        {
        }

        public AdventureEnemy(BestiaryEntry definition, int id)
        {
            this.definition = definition;
            this.id = id;

            this.texture = Master.texCollection.texEnemies;
            this.health = definition.health;
            this.graphicsRow = definition.graphicsRow;
            this.offset = new Vector2(definition.xOffset, definition.yOffset);
            this.width = definition.width;
            this.height = definition.height;
            this.ghost = definition.ghost;
        }

        public void ChangeEnemy(BestiaryEntry definition, int id)
        {
            this.definition = definition;
            this.id = id;
            if (health > definition.health)
                this.health = definition.health;
            this.graphicsRow = definition.graphicsRow;
            this.offset = new Vector2(definition.xOffset, definition.yOffset);
            this.width = definition.width;
            this.height = definition.height;
            this.ghost = definition.ghost;
        }

        public override void Update()
        {
            switch (definition.movementType)
            {
                default:
                    // Stationary
                    break;
                case BestiaryEntry.MovementTypes.random:
                    if (stallCount % (definition.speed) != 0)
                    {
                        if (Master.globalRandom.Next(0, 10) > definition.decisiveness)
                            faceDir = (Master.Directions)Master.globalRandom.Next(0, 4);
                        else
                        {
                            Vector2 move_dist = new Vector2(0, 0);
                            switch (faceDir)
                            {
                                case Master.Directions.Down:
                                    move_dist = new Vector2(0, 2);
                                    break;
                                case Master.Directions.Up:
                                    move_dist = new Vector2(0, -2);
                                    break;
                                case Master.Directions.Left:
                                    move_dist = new Vector2(-2, 0);
                                    break;
                                case Master.Directions.Right:
                                    move_dist = new Vector2(2, 0);
                                    break;
                                default:
                                    break; // Something has gone wrong
                            }
                            this.Move(move_dist);
                        }
                    }
                    break;
                case BestiaryEntry.MovementTypes.intelligent:
                    if (stallCount % (definition.speed) != 0)
                    {
                        if (Master.globalRandom.Next(0, 10) > definition.decisiveness)
                        {
                            if (Master.globalRandom.Next(0, 10) < definition.intelligence)
                            {
                                // Code from Aspect Star 1
                                int x_tar = (int)(location.X - parent.player.location.X);
                                int y_tar = (int)(location.Y - parent.player.location.Y);
                                Master.Directions targetDir;

                                if (parent.vloop)
                                    y_tar = (int)Master.absoluteMin(y_tar, location.Y - (parent.player.location.Y - 13 * 32), location.Y - (parent.player.location.Y + 13 * 32));
                                if (parent.hloop)
                                    x_tar = (int)Master.absoluteMin(x_tar, location.X - (parent.player.location.X - Master.width), location.X - (parent.player.location.X + Master.width));

                                targetDir = Math.Abs(x_tar) > Math.Abs(y_tar) ? x_tar > 0 ? Master.Directions.Left : Master.Directions.Right : y_tar > 0 ? Master.Directions.Up : Master.Directions.Down;
                                this.faceDir = targetDir;
                            }
                            else if (definition.wanderer)
                            {
                                Master.Directions newDir = (Master.Directions)Master.globalRandom.Next(0, 3);
                                switch (faceDir)
                                {
                                    case Master.Directions.Down:
                                        if (newDir == Master.Directions.Up)
                                            newDir = Master.Directions.Right;
                                        break;
                                    case Master.Directions.Up:
                                        if (newDir == Master.Directions.Down)
                                            newDir = Master.Directions.Right;
                                        break;
                                    case Master.Directions.Right:
                                        if (newDir == Master.Directions.Left)
                                            newDir = Master.Directions.Right;
                                        break;
                                }
                                faceDir = newDir;
                            }
                        }
                        else
                        {
                            Vector2 move_dist = new Vector2(0, 0);
                            switch (faceDir)
                            {
                                case Master.Directions.Down:
                                    move_dist = new Vector2(0, 2);
                                    break;
                                case Master.Directions.Up:
                                    move_dist = new Vector2(0, -2);
                                    break;
                                case Master.Directions.Left:
                                    move_dist = new Vector2(-2, 0);
                                    break;
                                case Master.Directions.Right:
                                    move_dist = new Vector2(2, 0);
                                    break;
                                default:
                                    break; // Something has gone wrong
                            }
                            this.Move(move_dist);
                        }
                    }
                    break;
            }

            base.Update();
        }

        public override void Draw(SpriteBatch spriteBatch, Color mask)
        {
            bool drawGhost = true;

            if (flickerCount > 0)
            {
                flickerCount--;
                if (flickerCount % 7 == 0)
                    mask = Color.Red;
                drawGhost = false;
            }

            if (ghost && Master.globalRandom.Next(0, 35) == 9)
                flickerCount = 6;

            if (!ghost || drawGhost)
                base.Draw(spriteBatch, mask);
        }

        public virtual void Hurt(bool ghostly, int damage)
        {
            if (definition != null && definition.dependent != "" && !parent.GetFlag(definition.dependent))
                return; // Flag "dependent" must be on

            if (ghostly == ghost && this.flickerCount == 0)
            {
                PlaySound.Play(PlaySound.SoundEffectName.Boom);
                health = health - (defense ? 1 : damage);
                flickerCount = flickerTime;
                if (health <= 0)
                {
                    active = false;
                    Die();
                }
            }
        }

        public virtual void Die()
        {
            parent.addObject(new AdventureExplosion(this.location));
            if (Master.globalRandom.Next(0, 10) <= 3)
            {
                AdventureItem aI = game.getRandomItem();
                aI.location = location;
                parent.addObject(aI);
            }
        }

        public override bool inRange(AdventurePlayer player)
        {
            Vector2 playerloc = player.location;

            //if (Math.Sqrt( Math.Pow(location.X - playerloc.X, 2) + Math.Pow(location.Y - playerloc.Y, 2) ) <= Math.Max(width, height))
            if (doesOverlap(player))
            {
                player.Hurt();
                player.Recoil(location, this);
                return true;
            }
            return false;
        }

        public override void Move(Vector2 move_dist)
        {
            if (interenemycollide || !parent.interenemyCollide(location + move_dist, radius, this))
                base.Move(move_dist);
        }

        public override void Touch()
        {
            // Enemies are defined by their dependence on inRange
        }

        public bool isStationary()
        {
            return !(definition == null)&&definition.movementType == BestiaryEntry.MovementTypes.stationary;
        }
    }
}
