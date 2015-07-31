﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aspectstar2
{
    public abstract class AdventureObject
    {
        protected Game game;
        protected AdventureScreen parent;
        protected Texture2D texture;
        
        protected int currentFrame = 0;
        int stallCount = 0;
        protected Vector2 offset = new Vector2(16,40);
        protected int width = 14;
        protected int height = 6;

        public Master.Directions faceDir;
        public bool moving;
        public Vector2 location;
        protected int z = 0;
        int vz = 0;

        public bool active = true;

        public void Initialize(AdventureScreen parent, Game game)
        {
            this.parent = parent;
            this.game = game;
        }

        public virtual void Update()
        {
            if (stallCount == 10)
            {
                stallCount = 0;
                currentFrame = currentFrame + 1;
                if (currentFrame == 2)
                    currentFrame = 0;
            }
            else
                stallCount++;

            if ((stallCount % 4 == 0))
            {
                z = z + vz;
                if (z > 0)
                    vz = vz - 1;
                else
                {
                    z = 0;
                    vz = 0;
                    if (parent.isSolid(this.location, 0, 0, 0))
                    {
                        moving = false;
                        parent.Drown();
                    }
                }
            }

            if ((stallCount % 4 != 0) && (moving))
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

        public virtual void Draw(SpriteBatch spriteBatch, Color mask)
        {
            int dim_x = 32;
            int dim_y = 48;
            int column = ((int)faceDir * 2) + currentFrame;
            Vector2 screen_loc = location - offset;

            Rectangle sourceRectangle = new Rectangle(dim_x * column, 0, dim_x, dim_y);
            Rectangle destinationRectangle = new Rectangle((int)screen_loc.X, (int)screen_loc.Y - (z * 2), dim_x, dim_y);

            spriteBatch.Begin();
            spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, mask);
            spriteBatch.End();
        }

        public virtual void Move(Vector2 move_dist)
        {
            Vector2 test = move_dist + location;
            if ((test.X - width) >= 0 && (test.Y - height) >= 0 && (test.X + width) < (25 * 32) && (test.Y + height) < (13 * 32) )
                if (!this.parent.isSolid(test, z, width, height))
                    this.location = test;
        }

        public virtual void Jump()
        {
            if (this.z == 0)
                this.vz = +5;
        }

        public abstract void Touch() ;

        public abstract void inRange(Vector2 playerloc) ;
    }

    class AdventurePlayer : AdventureObject
    {
        int flickerCount = 1;

        public AdventurePlayer(AdventureScreen parent, Game game)
        {
            this.texture = Master.texCollection.texAdvPlayer;
            this.parent = parent;
            this.game = game;
        }

        public override void Draw(SpriteBatch spriteBatch, Color mask)
        {

            int dim_x = 32;
            int dim_y = 48;
            int column = ((int)faceDir * 2) + currentFrame;
            Vector2 screen_loc = location - offset;

            Rectangle sourceRectangle = new Rectangle(dim_x * column, 0, dim_x, dim_y);
            Rectangle destinationRectangle = new Rectangle((int)screen_loc.X, (int)screen_loc.Y, dim_x, dim_y);

            spriteBatch.Begin();
            spriteBatch.Draw(Master.texCollection.texShadows, destinationRectangle, sourceRectangle, Color.White);
            spriteBatch.End();

            if (flickerCount > 0)
            {
                flickerCount--;
                if (flickerCount % 7 == 0)
                    mask = Color.Red;
            }
            base.Draw(spriteBatch, mask);
        }

        public void Drown(SpriteBatch spriteBatch, int animCount)
        {
            currentFrame++;
            int dim_x = 32;
            int dim_y = 48;
            int column = (currentFrame % 16 > 8) ? 0 : 1;
            Vector2 screen_loc = location - offset;

            Rectangle sourceRectangle = new Rectangle(dim_x * column, 48, dim_x, dim_y - ((24 - animCount) * 2));
            Rectangle destinationRectangle = new Rectangle((int)screen_loc.X, (int)screen_loc.Y + (24 - animCount) *2, dim_x, dim_y - ((24 - animCount)*2));

            spriteBatch.Begin();
            spriteBatch.Draw(Master.texCollection.texAdvPlayer, destinationRectangle, sourceRectangle, Color.White);
            spriteBatch.End();
        }

        public override void Jump()
        {
            if (z == 0)
                PlaySound.Jump();
            base.Jump();
        }

        public void Hurt()
        {
            if (flickerCount == 0)
            {
                game.life = game.life - 1;
                Flicker();
                PlaySound.Hurt();
            }

            if (game.life <= 0)
                parent.Die();
        }

        public void Flicker()
        {
            this.flickerCount = 80;
        }

        public override void Update()
        {
            base.Update();

            if (!this.moving)
                this.currentFrame = 0;
        }

        public override void Move(Vector2 move_dist)
        {
            Vector2 test = move_dist + location;
            if ((test.X - width) <= 0)
                parent.enterNewRoom(-1, 0);
            else if ((test.Y - height) <= 0)
                parent.enterNewRoom(0, -1);
            else if ((test.X + width) >= (25 * 32))
                parent.enterNewRoom(1, 0);
            else if ((test.Y + height) >= (13 * 32))
                parent.enterNewRoom(0, 1);
            else
            {
                base.Move(move_dist);
                if ((z == 0) && (parent.isInjury(test, width, height)))
                {
                    Hurt();
                }
                parent.tileAction(test, width, height);
            }
        }

        public override void Touch()
        {
            // Do nothing
        }

        public override void inRange(Vector2 playerloc)
        {
            // Do nothing
        }
    }
}
