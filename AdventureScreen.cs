﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Jint;

namespace aspectstar2
{
    public class AdventureScreen : Screen
    {
        readonly Game game;

        public bool fromMap;
        public bool beaten;

        public int tileset;

        public AdventurePlayer player;
        List<AdventureObject> objects = new List<AdventureObject>();
        List<AdventureObject> newobjects = new List<AdventureObject>();
        public Adventure adventure;
        int roomX, roomY;
        Vector2 first_pos;
        private int[] tileMap;
        private readonly int[] key;
        Engine jintEngine;

        public int Keys;

        string label;
        Color playerColor = Color.White;
        Color overlayColor = Color.White;

        public bool hloop;
        public bool vloop;
        public bool hblock;
        public bool vblock;
        public bool dark;
        public bool lit
        {
            get { return (torchCount > 0); }
            set
            {
                if (torchCount == 0)
                    PlaySound.Play(PlaySound.SoundEffectName.Aspect);
                torchCount += 900;
            }
        }
        int torchCount;

        int animCount, stallCount, lag;
        string textString; int choice = 1; Action<bool> chooser;
        Action<bool> leaver;

        public Action dieFunc;
        bool dead;

        adventureModes currentMode = adventureModes.runMode;
        enum adventureModes
        {
            runMode,
            drowning,
            scrollingX,
            scrollingY,
            deathFade,
            fadeOut,
            fadeIn,
            textBox,
        }

        enum tileType
        {
            Clear = 0,
            Solid = 1,
            Crevasse = 2,
            Injury = 3,
            Warp = 4,
            Heal = 5,
            Lock = 6,
            Wood = 7,
        }

        public AdventureScreen(Game game, int dest, int destroomX, int destroomY, int x, int y, bool beaten)
        {
            this.game = game;
            this.master = game.master;
            this.player = new AdventurePlayer(this, game);

            this.adventure = Master.currentFile.adventures[dest].Clone();
            this.tileset = adventure.tileset;
            this.key = adventure.key;
            this.beaten = beaten;
            label = adventure.name;
            LoadRoom(destroomX, destroomY);

            this.first_pos = new Vector2(x * 32 + 16, y * 32 + 16);
            player.location = new Vector2(x * 32 + 16, y * 32 + 16);
        }

        public bool isSolid(int x, int y)
        {
            int i = x + (y * 25);

            return (((tileType)key[tileMap[i]] == tileType.Solid) || ((tileType)key[tileMap[i]] == tileType.Lock)) || ((tileType)key[tileMap[i]] == tileType.Wood);
        }

        public bool isSolid(Vector2 dest, int z, int width, int height, Master.Directions faceDir)
        {
            // Checks for the solidity of a rectangle; width and height are measured from the center of the rectangle

            int i, x, y;

            if ((faceDir == Master.Directions.Up) || (faceDir == Master.Directions.Left))
            {
                x = (int)Math.Floor((dest.X - width) / 32);
                y = (int)Math.Floor((dest.Y - height) / 32);
                i = x + (y * 25);
                if (i >= tileMap.Length || i < 0)
                    return true;
                if (((tileType)key[tileMap[i]] == tileType.Solid) || ((tileType)key[tileMap[i]] == tileType.Lock) || ((tileType)key[tileMap[i]] == tileType.Wood) ||
                    (((tileType)key[tileMap[i]] == tileType.Crevasse) && z == 0))
                    return true;
            }

            if ((faceDir == Master.Directions.Up) || (faceDir == Master.Directions.Right))
            {
                x = (int)Math.Floor((dest.X + width) / 32);
                y = (int)Math.Floor((dest.Y - height) / 32);
                i = x + (y * 25);
                if (i >= tileMap.Length || i < 0)
                    return true;
                if (((tileType)key[tileMap[i]] == tileType.Solid) || ((tileType)key[tileMap[i]] == tileType.Lock) || ((tileType)key[tileMap[i]] == tileType.Wood) ||
                    (((tileType)key[tileMap[i]] == tileType.Crevasse) && z == 0))
                    return true;
            }

            if ((faceDir == Master.Directions.Down) || (faceDir == Master.Directions.Left))
            {
                x = (int)Math.Floor((dest.X - width) / 32);
                y = (int)Math.Floor((dest.Y + height) / 32);
                i = x + (y * 25);
                if (i >= tileMap.Length || i < 0)
                    return true;
                if (((tileType)key[tileMap[i]] == tileType.Solid) || ((tileType)key[tileMap[i]] == tileType.Lock) || ((tileType)key[tileMap[i]] == tileType.Wood) ||
                    (((tileType)key[tileMap[i]] == tileType.Crevasse) && z == 0))
                    return true;
            }

            if ((faceDir == Master.Directions.Down) || (faceDir == Master.Directions.Right))
            {
                x = (int)Math.Floor((dest.X + width) / 32);
                y = (int)Math.Floor((dest.Y + height) / 32);
                i = x + (y * 25);
                if (i >= tileMap.Length || i < 0)
                    return true;
                if (((tileType)key[tileMap[i]] == tileType.Solid) || ((tileType)key[tileMap[i]] == tileType.Lock) || ((tileType)key[tileMap[i]] == tileType.Wood) ||
                    (((tileType)key[tileMap[i]] == tileType.Crevasse) && z == 0))
                    return true;
            }

            return false;
        }

        public void tileAction(Vector2 dest, int width, int height)
        {
            int i, x, y;
            //Center
            x = (int)Math.Floor((dest.X) / 32);
            y = (int)Math.Floor((dest.Y) / 32);
            i = x + (y * 25);
            tileStepAction(i);

            //Top Left
            x = (int)Math.Floor((dest.X - width) / 32);
            y = (int)Math.Floor((dest.Y - height) / 32);
            i = x + (y * 25);
            tileTouchAction(i);

            // Bottom Left
            x = (int)Math.Floor((dest.X + width) / 32);
            y = (int)Math.Floor((dest.Y - height) / 32);
            i = x + (y * 25);
            tileTouchAction(i);

            // Top Right
            x = (int)Math.Floor((dest.X - width) / 32);
            y = (int)Math.Floor((dest.Y + height) / 32);
            i = x + (y * 25);
            tileTouchAction(i);

            // Bottom Right
            x = (int)Math.Floor((dest.X + width) / 32);
            y = (int)Math.Floor((dest.Y + height) / 32);
            i = x + (y * 25);
            tileTouchAction(i);
        }

        void tileStepAction(int i)
        {
            tileType tile = (tileType)key[tileMap[i]];
            if (tile == tileType.Warp && (player.z == 0 ||
                (tileMap.Length > i && (tileType)tileMap[i+1] == tileType.Warp) || (i > 0 && (tileType)tileMap[i-1] == tileType.Warp)))
            {
                PlaySound.Play(PlaySound.SoundEffectName.Leave);
                PlaySong.Play(PlaySong.SongName.None);
                leaver = x => game.warpAdventure(x);
                currentMode = adventureModes.fadeOut;
                animCount = 250;
            }
            else if (tile == tileType.Heal)
            {
                if (game.life != game.possibleLife)
                {
                    game.life = game.possibleLife;
                }
                PlaySound.Play(PlaySound.SoundEffectName.Heal);
                tileMap[i] = 0;
            }
        }

        void tileTouchAction(int i)
        {
            if (i < 0 || i >= tileMap.Length)
                return;

            tileType tile = (tileType)key[tileMap[i]];
            if (tile == tileType.Lock)
            {
                if (Keys > 0)
                {
                    Keys--;
                    PlaySound.Play(PlaySound.SoundEffectName.Aspect);
                    tileMap[i] = 0;
                }
            }

            int x, y, j;
            foreach (AdventureObject obj in objects)
            {
                x = (int)Math.Floor(obj.location.X / 32);
                y = (int)Math.Floor(obj.location.Y / 32);
                j = x + (y * 25);
                if (j == i)
                    obj.Touch();
            }
        }

        public bool Collide(int flickerCount)
        {
            bool collided = false;
            foreach (AdventureObject obj in objects)
            {
                if (obj.inRange(player) &&
                    (flickerCount <= 0 ||
                    (obj is AdventureEnemy && ((AdventureEnemy)obj).isStationary())))
                    collided = true;
            }
            tileAction(player.location, 10, 6);
            return collided;
        }

        public void Burn(Vector2 dest)
        {
            int i, x, y;
            x = (int)Math.Floor((dest.X) / 32);
            y = (int)Math.Floor((dest.Y) / 32);
            i = x + (y * 25);

            if ((tileType)key[tileMap[i]] == tileType.Wood)
            {
                tileMap[i] = 0;
                PlaySound.Play(PlaySound.SoundEffectName.Boom);
            }
        }

        public bool isInjury(Vector2 dest, int width, int height)
        {
            // Checks for the solidity of a rectangle; width and height are measured from the center of the rectangle

            int i, x, y;
            //Top Left
            x = (int)Math.Floor((dest.X - width) / 32);
            y = (int)Math.Floor((dest.Y - height) / 32);
            i = x + (y * 25);
            if ((tileType)key[tileMap[i]] == tileType.Injury)
                return true;

            // Bottom Left
            x = (int)Math.Floor((dest.X + width) / 32);
            y = (int)Math.Floor((dest.Y - height) / 32);
            i = x + (y * 25);
            if ((tileType)key[tileMap[i]] == tileType.Injury)
                return true;

            // Top Right
            x = (int)Math.Floor((dest.X - width) / 32);
            y = (int)Math.Floor((dest.Y + height) / 32);
            i = x + (y * 25);
            if ((tileType)key[tileMap[i]] == tileType.Injury)
                return true;


            // Bottom Right
            x = (int)Math.Floor((dest.X + width) / 32);
            y = (int)Math.Floor((dest.Y + height) / 32);
            i = x + (y * 25);
            if ((tileType)key[tileMap[i]] == tileType.Injury)
                return true;

            return false;
        }

        public void Drown()
        {
            if (dead)
                return;

            animCount = 24;
            currentMode = adventureModes.drowning;
            PlaySound.Play(PlaySound.SoundEffectName.Drown);
            foreach (AdventureObject obj in objects)
            {
                if (obj is AdventureBoss7)
                {
                    AdventureBoss7 ab7 = (AdventureBoss7)obj;
                    ab7.location = new Vector2(Master.width / 2, 3 * 32);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            switch (currentMode)
            {
                case adventureModes.textBox:
                    DrawRoom(spriteBatch, Color.White);
                    List<AdventureObject> sortList = objects.OrderBy(o => o.location.Y).ToList();
                    foreach (AdventureObject obj in sortList)
                    {
                        if (obj is AdventurePlayer)
                        {
                            if (!dead)
                                obj.Draw(spriteBatch, playerColor);
                        }
                        else
                        {
                            if (dead)
                            {
                                if (obj is AdventureEntity)
                                    obj.Draw(spriteBatch, Color.White);
                            }
                            else
                                obj.Draw(spriteBatch, Color.White);
                        }
                    }
                    DrawTextBox(spriteBatch, textString);
                    if (choice != -1)
                    {
                        WriteText(spriteBatch, "YES", new Vector2((Master.width / 2) - (16 * 11), 16 * 6), Color.White);
                        WriteText(spriteBatch, "NO", new Vector2((Master.width / 2) + (16 * 8), 16 * 6), Color.White);

                        spriteBatch.Begin();
                        Rectangle source = new Rectangle(128, 16, 16, 16);
                        Rectangle dest;

                        dest = choice == 0 ? new Rectangle((Master.width / 2) - (16 * 13), 16 * 6, 16, 16) : new Rectangle((Master.width / 2) + (16 * 6), 16 * 6, 16, 16);

                        spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);
                        spriteBatch.End();
                    }
                    break;
                case adventureModes.drowning:
                    if (animCount == 0)
                    {
                        DrawRoom(spriteBatch, Color.White);
                        DrawStatus(spriteBatch);
                        if (game.life > 2)
                        {
                            game.life = game.life - 2;
                            player.location = first_pos;
                            player.Flicker();
                            currentMode = adventureModes.runMode;
                        }
                        else
                        {
                            game.life = 0;
                            Die();
                        }
                        return;
                    }

                    DrawRoom(spriteBatch, Color.White);
                    player.Drown(spriteBatch, animCount);

                    if (stallCount > 0)
                    {
                        stallCount = stallCount - 1;
                    }
                    else
                    {
                        stallCount = 3;
                        animCount--;
                    }
                    break;
                case adventureModes.deathFade:
                    Color mask = Color.White;
                    mask.G = (byte)(animCount / 2);
                    mask.B = (byte)(animCount / 2);
                    mask.R = (byte)(animCount);
                    DrawRoom(spriteBatch, mask);
                    break;
                case adventureModes.fadeOut:
                    Color mask2 = Color.White;
                    mask2.R = (byte)(animCount / 2);
                    mask2.G = (byte)(animCount / 2);
                    mask2.B = (byte)(animCount / 2);
                    DrawRoom(spriteBatch, mask2);
                    if (!dead)
                        player.Draw(spriteBatch, mask2);
                    else
                    {
                        foreach (AdventureObject obj in objects)
                        {
                            if (obj is AdventureEntity)
                                obj.Draw(spriteBatch, mask2);
                        }
                    }
                    break;
                case adventureModes.fadeIn:
                    Color mask3 = Color.White;
                    mask3.R = (byte)(255 - animCount / 2);
                    mask3.G = (byte)(255 - animCount / 2);
                    mask3.B = (byte)(255 - animCount / 2);
                    DrawRoom(spriteBatch, mask3);
                    player.Draw(spriteBatch, mask3);
                    break;
                case adventureModes.scrollingX:
                case adventureModes.scrollingY:
                    scroll(spriteBatch);
                    break;
                case adventureModes.runMode:
                    //Color roomColor = (!dark || lit) ? Color.White : Color.FromNonPremultiplied(15,15,15,255);
                    Color roomColor = overlayColor;
                    bool usePlayerColor = (playerColor != Color.White);
                    if (dark)
                    {
                        usePlayerColor = false;
                        roomColor = lit ? Color.FromNonPremultiplied(200, 200, 200, 255) : Color.FromNonPremultiplied(15, 15, 15, 255);
                    }
                    DrawRoom(spriteBatch, roomColor);
                    List<AdventureObject> sortedList = objects.OrderBy(o => o.location.Y).ToList();
                    foreach (AdventureObject obj in sortedList)
                    {
                        if (obj is AdventurePlayer)
                            obj.Draw(spriteBatch, usePlayerColor ? playerColor : roomColor);
                        else if (obj.active)
                            obj.Draw(spriteBatch, roomColor);
                    }
                    break;
            }
            DrawStatus(spriteBatch);
        }

        public void DrawRoom(SpriteBatch spriteBatch, Color mask)
        {
            int x, y;
            Rectangle source, dest;
            Vector2 sourceTile;
            spriteBatch.Begin();
            for (int i = 0; i < (tileMap.Length); i++)
            {
                x = i % 25;
                y = i / 25;
                sourceTile = Master.getMapTile(tileMap[i], Master.texCollection.adventureTiles[adventure.tileset]);
                source = new Rectangle((int)sourceTile.X, (int)sourceTile.Y, 32, 32);
                dest = new Rectangle(x * 32, y * 32, 32, 32);
                spriteBatch.Draw(Master.texCollection.adventureTiles[adventure.tileset], dest, source, mask);
            }
            spriteBatch.End();
        }

        public void DrawStatus(SpriteBatch spriteBatch)
        {
            int x, y;
            Rectangle source, dest;
            spriteBatch.Begin();

            // Controls icons
            x = 18;
            y = 13;
            source = new Rectangle(0, 0, 64, 64);
            dest = new Rectangle(x * 32, y * 32, 64, 64);
            spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);

            x = 21;
            y = 13;
            source = new Rectangle(64, 0, 64, 64);
            dest = new Rectangle(x * 32, y * 32, 64, 64);
            spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);

            // A icon
            //source = new Rectangle(0, 64, 32, 32);
            //dest = new Rectangle((int)(18.5 * 32), (int)(13.5 * 32), 32, 32);
            //spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);


            // Hearts
            int hearts = (int)Math.Ceiling((double)game.possibleLife / 2);
            for (int i = 1; i <= hearts; i++)
            {
                if (game.life == (i * 2 - 1))
                    source = new Rectangle((128 + 16), 0, 16, 16);
                else if ((i * 2) > game.life)
                    source = new Rectangle((128 + 32), 0, 16, 16);
                else
                    source = new Rectangle(128, 0, 16, 16);
                dest = new Rectangle((int)(1 * 32 + (i * 16)), (int)(13 * 32 + 16), 16, 16);
                spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);
            }

            // Bells
            source = new Rectangle(208, 0, 16, 16);
            dest = new Rectangle((int)(12 * 32), (int)(12 * 32 + 48), 16, 16);
            spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);

            // Keys
            source = new Rectangle((128 + 48), 0, 16, 16);
            dest = new Rectangle((int)(12 * 32), (int)(13 * 32 + 32), 16, 16);
            spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);
            source = new Rectangle((128 + 64), 0, 16, 16);
            dest = new Rectangle((int)(12 * 32 + (16 * 4)), (int)(13 * 32 + 32), 16, 16);
            spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);

            if (label == adventure.name)
            {
                // Beat level marker
                if (beaten)
                {
                    source = new Rectangle((128 + 48), 0, 16, 16);
                    dest = new Rectangle(48, 14 * 32, 16, 16);
                    spriteBatch.Draw(Master.texCollection.controls, dest, source, Color.White);
                }
                spriteBatch.End();

                // Name
                WriteText(spriteBatch, (beaten ? label.Substring(1) : label), new Vector2(48 + (beaten ? 16 : 0), 14 * 32), Color.White);
            }
            else
            {
                spriteBatch.End();
                WriteText(spriteBatch, label, new Vector2(48, 14 * 32), Color.White);
            }

            // Key counts
            WriteText(spriteBatch, game.bells.ToString(), new Vector2(12 * 32 + 16, 13 * 32 + 16), Color.White);
            WriteText(spriteBatch, game.goldKeys.ToString(), new Vector2(12 * 32 + 16, 14 * 32), Color.White);
            WriteText(spriteBatch, Keys.ToString(), new Vector2(12 * 32 + 16 * 5, 14 * 32), Color.White);

            // Weapons
            game.weaponA.Draw(spriteBatch, (int)(18.5 * 32), (int)(13.5 * 32));
            game.weaponB.Draw(spriteBatch, (int)(21.5 * 32), (int)(13.5 * 32));
        }

        void scroll(SpriteBatch spriteBatch)
        {
            bool scrollDown = true;
            if (Math.Sign(animCount) == -1)
                scrollDown = false;

            bool Xscroll = false;
            if (currentMode == adventureModes.scrollingX)
                Xscroll = true;

            Vector2 screenOffset;
            Rectangle source;
            Rectangle dest;
            Vector2 sourceTile, limitOffset;
            int x, y;
            spriteBatch.Begin();

            int width, height;

            if (Xscroll)
            {
                width = 50;
                height = 13;
                screenOffset.Y = 0;
                screenOffset.X = scrollDown ? ((width / 2) - animCount) * 32 : -(animCount) * 32;
            }
            else
            {
                width = 25;
                height = 26;
                screenOffset.X = 0;
                screenOffset.Y = scrollDown ? ((height / 2) - animCount) * 32 : -(animCount) * 32;
            }

            int[] newroom;
            int[] oldroom;
            int[] room = new int[width * height];

            if (Xscroll)
            {
                newroom = adventure.rooms[roomX, roomY].tileMap;
                oldroom = adventure.rooms[roomX - Math.Sign(animCount), roomY].tileMap;
                room = scrollDown ? Master.sumRooms(oldroom, newroom, 25, 13) : Master.sumRooms(newroom, oldroom, 25, 13);
            }
            else
            {
                newroom = adventure.rooms[roomX, roomY].tileMap;
                oldroom = adventure.rooms[roomX, roomY - Math.Sign(animCount)].tileMap;
                if (scrollDown)
                {
                    oldroom.CopyTo(room, 0);
                    newroom.CopyTo(room, 13 * 25);
                }
                else
                {
                    oldroom.CopyTo(room, 13 * 25);
                    newroom.CopyTo(room, 0);
                }
            }

            for (int i = 0; i < (width * height); i++)
            {
                x = i % width;
                y = i / width;
                limitOffset = new Vector2((float)Math.Floor(screenOffset.X / 32), (float)Math.Floor(screenOffset.Y / 32));
                if ((x >= limitOffset.X) && (x <= limitOffset.X + Master.width) &&
                    (y >= limitOffset.Y) && (y <= limitOffset.Y + (Master.height)))
                {
                    sourceTile = Master.getMapTile(room[i], Master.texCollection.adventureTiles[adventure.tileset]);
                    source = new Rectangle((int)sourceTile.X, (int)sourceTile.Y, 32, 32);
                    dest = new Rectangle(x * 32 - (int)screenOffset.X, y * 32 - (int)screenOffset.Y, 32, 32);
                    if (y - (int)limitOffset.Y < 13)
                        spriteBatch.Draw(Master.texCollection.adventureTiles[adventure.tileset], dest, source, overlayColor);
                }
            }
            spriteBatch.End();


            if (animCount == 0)
            {
                currentMode = adventureModes.runMode;
                stallCount = 0;
                return;
            }

            if (stallCount > 0)
            {
                stallCount = stallCount - 1;
            }
            else
            {
                stallCount = Xscroll ? 2 : 3;
                animCount = (Math.Abs(animCount) - 1) * Math.Sign(animCount);
            }
        }

        public void enterNewRoom(int del_x, int del_y)
        {
            int x = roomX + del_x;
            int y = roomY + del_y;

            if (x >= 0 && y >= 0 && x < 16 && y < 16)
                if (adventure.rooms[x, y] != null)
                {
                    if (del_x == -1)
                        player.location.X = 25 * 32 - 14 - 2;
                    else if (del_y == -1)
                        player.location.Y = 13 * 32 - 6 - 2;
                    else if (del_x == 1)
                        player.location.X = 14 + 2;
                    else if (del_y == 1)
                        player.location.Y = 6 + 2;
                    this.first_pos = player.location;

                    LoadRoom(x, y);

                    if (del_y != 0)
                    {
                        currentMode = adventureModes.scrollingY;
                        animCount = 12 * del_y;
                    }
                    else if (del_x != 0)
                    {
                        currentMode = adventureModes.scrollingX;
                        animCount = 24 * del_x;
                    }
                }

        }

        public void EnterNewRoom(int room_x, int room_y, int x, int y)
        {
            LoadRoom(room_x, room_y);
            first_pos = new Vector2(x * 32 + 16, y * 32 + 16);
            player.location = new Vector2(x * 32 + 16, y * 32 + 16);
            player.faceDir = (y > 6 ? Master.Directions.Down : Master.Directions.Up);
            animCount = 250;
            currentMode = adventureModes.fadeIn;
        }

        public void LoadRoom(int x, int y)
        {
            roomX = x; roomY = y;
            Room newRoom = adventure.rooms[x, y];
            this.tileMap = newRoom.tileMap;
            this.objects = new List<AdventureObject>();
            this.newobjects = new List<AdventureObject>();
            this.dark = newRoom.dark;
            this.hloop = false;
            this.vloop = false;
            this.hblock = newRoom.hblock;
            this.vblock = newRoom.vblock;
            objects.Add(player);
            foreach (AdventureObject aO in newRoom.adventureObjects)
            {
                aO.Initialize(this, game);
                objects.Add(aO);
            }

            PlaySong.Play(newRoom.music != -1 ? (PlaySong.SongName)newRoom.music : (PlaySong.SongName)adventure.music);

            // Run room code
            ActivateScript();
        }

        public override void Update(GameTime gameTime)
        {
            switch (currentMode)
            {
                case adventureModes.textBox:
                    if (stallCount > 0)
                        stallCount--;
                    if (stallCount <= 0 &&  (Master.controls.A || Master.controls.B))
                    {
                        currentMode = adventureModes.runMode;
                        stallCount = 20;
                        chooser(choice == 0);
                    }
                    else if (choice != -1 && Master.controls.Left)
                    {
                        choice = 0;
                    }
                    else if (choice != -1 && Master.controls.Right)
                    {
                        choice = 1;
                    }
                    break;
                case adventureModes.fadeOut:
                    animCount = animCount - 2;
                    if (animCount <= 0)
                        leaver(beaten);
                    //game.warpAdventure(beaten);
                    break;
                case adventureModes.fadeIn:
                    animCount = animCount - 8;
                    if (animCount <= 0)
                        currentMode = adventureModes.runMode;
                    //game.warpAdventure(beaten);
                    break;
                case adventureModes.deathFade:
                    animCount = animCount - 3;
                    if (animCount <= 0)
                    {
                        dead = true;
                        if (dieFunc != null)
                            dieFunc();
                        else
                            game.warpAdventure(beaten);
                    }
                    break;
                case adventureModes.runMode:
                    UpdateRoom();
                    break;
                default:
                    break;
            }
        }

        public void UpdateRoom()
        {
            if (jintEngine != null)
                jintEngine.Execute("update()");

            if (torchCount > 1)
            {
                torchCount = torchCount - 1;
            }
            else if (torchCount == 1)
            {
                torchCount = 0;
                PlaySound.Play(PlaySound.SoundEffectName.Boom);
            }

            foreach (AdventureObject obj in this.objects)
            {
                obj.Update();
            }
            foreach (AdventureObject obj in this.objects)
            {
                if (obj is AdventureEntity)
                    ((AdventureEntity)obj).enemyInRange(objects);
            }
            //if (!player.moving)
            Collide(player.flickerCount);
            objects.RemoveAll(isInactive);
            objects = objects.Concat(newobjects).ToList();
            newobjects = new List<AdventureObject>();

            if (currentMode == adventureModes.drowning)
                objects.RemoveAll(isProjectile);

            foreach (AdventureProjectile proj in this.objects.Where(isProjectile))
            {
                proj.projectileUpdate(objects);
            }

            // Get keyboard input
            //KeyboardState state = Keyboard.GetState();

            if (Master.controls.Up)
            {
                player.moving = true;
                player.faceDir = Master.Directions.Up;
            }
            else if (Master.controls.Down)
            {
                player.moving = true;
                player.faceDir = Master.Directions.Down;
            }
            else if (Master.controls.Left)
            {
                player.moving = true;
                player.faceDir = Master.Directions.Left;
            }
            else if (Master.controls.Right)
            {
                player.moving = true;
                player.faceDir = Master.Directions.Right;
            }
            else
            {
                player.moving = false;
            }

            if (stallCount <= 0)
            {
                if (Master.controls.A)
                    game.weaponA.Activate(player, this, game);
                else if (Master.controls.B)
                    game.weaponB.Activate(player, this, game);
            }
            else
                stallCount--;


            if (Master.controls.Start && lag == 0)
            {
                game.Pause();
                lag = 20;
            }
            if (lag != 0)
                lag = lag - 1;

            game.weaponA.Update();
            game.weaponB.Update();
        }

        public void Die()
        {
            animCount = 250;
            currentMode = adventureModes.deathFade;
            PlaySound.Play(PlaySound.SoundEffectName.Die);
            PlaySong.Play(PlaySong.SongName.Silent);
        }

        public void addObject(AdventureObject obj)
        {
            obj.Initialize(this, game);
            newobjects.Add(obj);
        }

        void DrawTextBox(SpriteBatch spriteBatch, string text)
        {
            int height = 4 * 32 + 16;
            spriteBatch.Begin();
            spriteBatch.Draw(Master.texCollection.blank, new Rectangle(0, 0, Master.width, height), Color.Black);
            spriteBatch.End();

            text = text.ToUpper();
            int tiles = 46; // (int)Math.Floor((double)(Master.width - 64) / 16);
            bool moreText = true; int i = 0; string dummy;
            while (moreText)
            {
                if ((text.Length - (i * tiles)) >= tiles)
                    dummy = text.Substring(i * tiles, tiles);
                else
                {
                    dummy = text.Substring(i * tiles, text.Length - (i * tiles));
                    moreText = false;
                }
                WriteText(spriteBatch, dummy, new Vector2(32, 32 + (i * 32)), Color.White);

                i++;
            }
        }

        public void leaveAdventure(int dest, int destx, int desty, int destroomX, int destroomY)
        {
            leaver = x => game.exitAdventure(x, dest, destroomX, destroomY, destx, desty);
            currentMode = adventureModes.fadeOut;
            animCount = 250;
            PlaySound.Play(PlaySound.SoundEffectName.Leave);
        }

        public void enterSpecialStage(int screen, int key)
        {
            PlaySound.Play(PlaySound.SoundEffectName.Special);
            leaver = x => game.enterSpecialStageFromAdventure(screen, key);
            currentMode = adventureModes.fadeOut;
            animCount = 250;
        }

        public void FadeIn()
        {
            animCount = 250;
            currentMode = adventureModes.fadeIn;
        }

        public bool interenemyCollide(Vector2 location, int radius, AdventureObject self)
        {
            if (self is AdventureEnemy && ((AdventureEnemy)self).ghost)
                return false;

            foreach (AdventureObject obj in objects)
            {
                if (obj is AdventureEnemy && obj != self && !((AdventureEnemy)obj).ghost)
                {
                    if (Vector2.Distance(obj.location, location) < radius)
                        return true;
                }
            }
            return false;
        }

        public Engine ActivateEngine(string code)
        {
            return new Engine(cfg => cfg.AllowClr())
                .SetValue("spawnKey", new Action<int, int>(this.SpawnKey))
                .SetValue("spawnGoldKey", new Action<int, int>(this.SpawnGoldKey))
                .SetValue("getFlag", new Func<string, bool>(this.GetFlag))
                .SetValue("setFlag", new Action<string, bool>(this.SetFlag))
                .SetValue("overwriteTile", new Action<int, int, int>(this.OverwriteTile))
                .SetValue("setSpawn", new Action<int, int>(this.SetSpawn))
                .SetValue("playSoundEffect", new Action<int>(this.PlaySoundEffect))
                .SetValue("playBGM", new Action<int>(this.PlayBGM))
                .SetValue("anyEnemies", new Func<bool>(this.AnyEnemies))
                .SetValue("clearObjects", new Action(this.ClearObjects))
                .SetValue("TextBox", new Action<string>(this._textBox))
                .SetValue("callMethod", new Action<string, string>(this.CallMethod))
                .SetValue("giveWeapon", new Action<int>(this.GiveWeapon))
                .SetValue("getPlayerX", new Func<int>(this.GetPlayerX))
                .SetValue("getPlayerY", new Func<int>(this.GetPlayerY))
                .SetValue("setCounter", new Action<string, int>(this.SetCounter))
                .SetValue("getCounter", new Func<string, int>(this.GetCounter))
                .SetValue("setPlayerColor", new Action<int, int, int, int>(this.SetPlayerColor))
                .SetValue("setOverlayColor", new Action<int, int, int, int>(this.SetOverlayColor))
                .SetValue("setPlayerRow", new Action<int>(SetPlayerRow))
                .SetValue("setName", new Action<string>(this.SetName))
                .SetValue("spawnBoss", new Action<int, int>(this.SpawnBoss))
                .SetValue("spawnFinal", new Action<int, int>(this.SpawnFinal))
                .SetValue("spawnEnemy", new Action<int, int, int>(this.SpawnEnemy))
                .SetValue("teleport", new Action<int, int, int, int>(this.EnterNewRoom))
                .SetValue("getEnemyId", new Func<int, int>(this.GetEnemyId))
                .SetValue("changeEnemy", new Action<int, int>(this.ChangeEnemy))
                .SetValue("enterCredits", new Action(this.EnterCredits))
                .Execute(code);
        }

        void ActivateScript()
        {
            string code = adventure.rooms[roomX, roomY].code;
            if (code != null)
            {
                jintEngine = ActivateEngine(code);

                jintEngine.Execute("onLoad()");
            }
            else
                jintEngine = null;
        }

        // Relevant to code

        void SpawnKey(int x, int y)
        {
            AdventureKey aK = new AdventureKey();
            aK.Initialize(this, game);
            aK.location.X = 32 * x + 16;
            aK.location.Y = 32 * y + 16;
            newobjects.Add(aK);
            adventure.rooms[roomX, roomY].adventureObjects.Add(aK); // So that the key respawns even if you leave the room, but not the adventure
            PlaySound.Play(PlaySound.SoundEffectName.Aspect);

        }

        void SpawnGoldKey(int x, int y)
        {
            AdventureGoldKey aK = new AdventureGoldKey();
            aK.Initialize(this, game);
            aK.location.X = 32 * x + 16;
            aK.location.Y = 32 * y + 16;
            newobjects.Add(aK);
            adventure.rooms[roomX, roomY].adventureObjects.Add(aK); // So that the key respawns even if you leave the room, but not the adventure
        }

        public void OverwriteTile(int x, int y, int newTile)
        {
            int i = x + (y * 25);
            tileMap[i] = newTile;
            if (Math.Floor(first_pos.X / 32) == x && Math.Floor(first_pos.Y / 32) == y)
                first_pos = new Vector2(32 * (float)Math.Floor(player.location.X / 32), 32 * (float)Math.Floor(player.location.Y / 32));
        }

        void SetSpawn(int x, int y)
        {
            first_pos = new Vector2(32 * x + 16, 32 * y + 16);
        }

        void PlaySoundEffect(int i)
        {
            PlaySound.Play((PlaySound.SoundEffectName)i);
        }

        void PlayBGM(int i)
        {
            PlaySong.Play((PlaySong.SongName)i);
        }

        bool AnyEnemies()
        {
            foreach (AdventureObject aO in objects)
            {
                if (aO is AdventureEnemy)
                    return true;
            }
            return false;
        }

        public void ClearObjects()
        {
            foreach (AdventureObject obj in objects)
            {
                if (!(obj is AdventurePlayer) && !(obj is AdventureExplosion) && !(obj is AdventureTeleporter))
                    obj.active = false;
                if ((obj is AdventureEnemy) || (obj is AdventureShooter) || (obj is AdventureBoss9Phase2) || (obj is AdventureBoss8Helper))
                {
                    addObject(new AdventureExplosion(obj.location));
                }
            }
        }

        readonly Queue<string> textStack = new Queue<string>();

        void _textBox(string text)
        {
            TextBox(text, true);
        }

        public void TextBox(string text, bool replace)
        {
            if (currentMode != adventureModes.textBox)
            {
                textString = text;
                choice = -1;
                currentMode = adventureModes.textBox;
                stallCount = 20;
            }
            else
            {
                textStack.Enqueue(text);
            }

            if (replace)
            {
                chooser = delegate
                {
                    if (textStack.Count != 0)
                        TextBox(textStack.Dequeue(), false);
                };
            }
        }

        public void QuestionBox(string text, Action<bool> choose)
        {
            if (currentMode != adventureModes.textBox)
            {
                textString = text;
                choice = 1;
                chooser = delegate (bool b)
                {
                    choose(b);
                };
                stallCount = 20;
                currentMode = adventureModes.textBox;
            }
            else
            {
                textStack.Enqueue(text);
                chooser = delegate
                {
                    if (textStack.Count == 1)
                        QuestionBox(textStack.Dequeue(), choose);
                    else
                        TextBox(textStack.Dequeue(), false);
                };
            }
        }

        void CallMethod(string name, string message)
        {
            AdventureEntity ent;

            foreach (AdventureObject obj in objects)
                if (obj is AdventureEntity)
                {
                    ent = (AdventureEntity)obj;
                    if (ent.name == name)
                        ent.Execute(message);
                }
        }

        void GiveWeapon(int weapon)
        {
            switch (weapon)
            {
                case 0:
                    game.GetWeapon(new ProjectileWeapon());
                    break;
                case 1:
                    game.GetWeapon(new FishWeapon());
                    break;
                case 2:
                    game.GetWeapon(new TorchWeapon());
                    break;
                case 3:
                    game.GetWeapon(new FireballWeapon());
                    break;
                case 4:
                    game.GetWeapon(new GhostlyWeapon());
                    break;
                case 5:
                    game.GetWeapon(new FarWeapon());
                    break;
                case 6:
                    game.GetWeapon(new CatnipWeapon());
                    break;
                case 7:
                    game.GetWeapon(new PillWeapon());
                    break;
            }
        }

        int GetPlayerX()
        {
            return (int)Math.Floor(player.location.X / 32);
        }

        int GetPlayerY()
        {
            return (int)Math.Floor(player.location.Y / 32);
        }

        void SetPlayerColor(int r, int g, int b, int a)
        {
            playerColor = Color.FromNonPremultiplied(r, g, b, a);
        }

        void SetOverlayColor(int r, int g, int b, int a)
        {
            overlayColor = Color.FromNonPremultiplied(r, g, b, a);
        }

        void SetPlayerRow(int i)
        {
            player.row = i;
        }

        public void SetName(string name)
        {
            label = name != "" ? name : adventure.name;
        }

        readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

        public void SetFlag(string flag, bool value)
        {
            if (flag.StartsWith("!"))
            {
                if (game.globalFlags.ContainsKey(flag))
                    game.globalFlags.Remove(flag);
                game.globalFlags.Add(flag, value);
            }
            else if (flag == "_dark")
            {
                dark = value;
                adventure.rooms[roomX, roomY].dark = value;
            }
            else if (flag == "_hloop")
            {
                hloop = value;
            }
            else if (flag == "_vloop")
            {
                vloop = value;
            }
            else if (flag == "_vblock")
            {
                vblock = value;
                adventure.rooms[roomX, roomY].vblock = value;
            }
            else if (flag == "_hblock")
            {
                hblock = value;
                adventure.rooms[roomX, roomY].hblock = value;
            }
            else
            {
                if (flags.ContainsKey(flag))
                    flags.Remove(flag);
                flags.Add(flag, value);
            }
        }

        public bool GetFlag(string flag)
        {
            if (flag.StartsWith("!"))
            {
                if (game.globalFlags.ContainsKey(flag))
                    return game.globalFlags[flag];
            }
            else if (flag == "_dark")
            {
                return dark;
            }
            else if (flag == "_beaten")
            {
                return beaten;
            }
            else
            {
                if (flags.ContainsKey(flag))
                    return flags[flag];
            }
            return false;
        }

        readonly Dictionary<string, int> counters = new Dictionary<string, int>();

        public void SetCounter(string flag, int value)
        {
            if (flag == "_bells")
                game.bells = value;
            else if (flag == "_life")
            {
                game.life = value;
                if (game.life > game.possibleLife)
                    game.life = game.possibleLife;
            }
            else if (flag == "_possibleLife")
            {
                game.possibleLife = value;
                if (game.life > game.possibleLife)
                    game.life = game.possibleLife;
            }
            else if (flag == "_keys")
                Keys = value;
            else if (flag == "_goldKeys")
                game.goldKeys = value;
            else
            {
                if (counters.ContainsKey(flag))
                    counters.Remove(flag);
                counters.Add(flag, value);
            }
        }

        int GetCounter(string flag)
        {
            if (flag == "_bells")
                return game.bells;
            else if (flag == "_life")
                return game.life;
            else if (flag == "_possibleLife")
                return game.possibleLife;
            else if (flag == "_crystalKeys")
                return game.crystalKeyCount;
            else if (flag == "_keys")
                return Keys;
            else if (flag == "_goldKeys")
                return game.goldKeys;
            else if (flag == "_deaths")
                return game.deaths;
            else if (counters.ContainsKey(flag))
                return counters[flag];
            else
                return 0;
        }

        void SpawnBoss(int x, int y)
        {
            AdventureBoss1 boss1 = new AdventureBoss1();
            boss1.location = new Vector2(x * 32 + 16, y * 32 + 16);
            addObject(boss1);
        }

        void SpawnFinal(int x, int y)
        {
            AdventureBoss9 boss9 = new AdventureBoss9();
            boss9.location = new Vector2(x * 32 + 16, y * 32 + 16);
            addObject(boss9);
        }

        void SpawnEnemy(int x, int y, int enemy)
        {
            AdventureEnemy aE = new AdventureEnemy(Master.currentFile.bestiary[enemy], enemy);
            aE.location = new Vector2(x * 32 + 16, y * 32 + 16);
            addObject(aE);
        }

        int GetEnemyId(int enemy)
        {
            return objects[enemy] is AdventureEnemy ? ((AdventureEnemy)objects[enemy]).id : -2;
        }

        void ChangeEnemy(int enemy, int definition)
        {
            if (objects[enemy] is AdventureEnemy)
                ((AdventureEnemy)objects[enemy]).ChangeEnemy(Master.currentFile.bestiary[definition], definition);
        }

        void EnterCredits()
        {
            leaver = x => game.enterCredits();
            currentMode = adventureModes.fadeOut;
            animCount = 250;
        }

        // Predicates

        public static bool isInactive(AdventureObject obj)
        {
            return !(obj.active);
        }

        public static bool isProjectile(AdventureObject proj)
        {
            return proj is AdventureProjectile;
        }
    }
}
