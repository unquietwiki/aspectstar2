﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace aspectstar2
{
    class PauseScreen : Screen
    {
        readonly AdventureScreen screen;
        readonly Game game;
        int selection;
        int lag = 10;
        int animCount;

        public PauseScreen(AdventureScreen screen, Game game)
        {
            this.screen = screen;
            this.game = game;
            PlaySound.Play(PlaySound.SoundEffectName.Pause);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!screen.dark || screen.lit)
                screen.DrawRoom(spriteBatch, Color.FromNonPremultiplied(255, 255, 255, 100));

            WriteText(spriteBatch, "ABILITIES AND WEAPONS", new Vector2(32, 32), Color.White);

            int i = 0;
            foreach (Weapon w in game.weapons)
            {

                spriteBatch.Begin();
                spriteBatch.Draw(Master.texCollection.blank, new Rectangle(32 + (i * (32 + 16)), 64 + 16, 32, 32), (i == selection) ? Color.DarkRed : Color.Black);
                spriteBatch.End();

                w.Draw(spriteBatch, 32 + (i * (32 + 16)), 64 + 16);

                if (w == game.weaponA)
                    WriteText(spriteBatch, "A", new Vector2(32 + (i * (32 + 16)), 64), Color.White);
                else if (w == game.weaponB)
                    WriteText(spriteBatch, "B", new Vector2(32 + (i * (32 + 16)), 64), Color.White);
                i++;
            }

            WriteText(spriteBatch, game.weapons[selection].getLabel(), new Vector2(32, 128), Color.White);

            if (game.crystalKeyCount > 0)
            {
                WriteText(spriteBatch, "CRYSTAL KEYS", new Vector2(32, 256 + 80), Color.White);
                for (int j = 0; j < game.crystalKeys.Length; j++)
                {
                    if (game.crystalKeys[j])
                    {
                        spriteBatch.Begin();
                        Rectangle source = new Rectangle(128, 32, 32, 32);
                        Rectangle dest = new Rectangle(32 + j * (32 + 16), 256 + 80 + 32, 32, 32);
                        spriteBatch.Draw(Master.texCollection.controls, dest, source, getCrystalMask(animCount + 20 * j));
                        spriteBatch.End();
                    }
                }
            }

            screen.DrawStatus(spriteBatch);
        }

        public static Color getCrystalMask(int animCount)
        {
            while (animCount < 0)
                animCount = animCount + 160;

            while (animCount > 160)
                animCount = animCount - 160;

            Color crystalMask = Color.White;
            if (animCount <= 120)
            {
                if (animCount > 80)
                    crystalMask.R = 0;
                else if (animCount > 40)
                    crystalMask.G = 0;
                else
                    crystalMask.B = 0;
            }

            return crystalMask;
        }

        public override void Update(GameTime gameTime)
        {
            if (animCount == 0)
                animCount = 160;
            else animCount--;

            if (lag > 0)
                lag = lag - 1;
            else
            {
                //KeyboardState state = Keyboard.GetState();
                if (Master.controls.Left)
                {
                    selection = selection - 1;
                    if (selection < 0)
                        selection = 0;
                    lag = 15;
                }
                else if (Master.controls.Right)
                {
                    selection = selection + 1;
                    if (selection == game.weapons.Count)
                        selection = selection - 1;
                    lag = 15;
                }
                else if (Master.controls.A)
                {
                    game.weaponA = game.weapons[selection];
                    if (game.weaponA == game.weaponB)
                        game.weaponB = new NullWeapon();
                    lag = 15;
                }
                else if (Master.controls.B)
                {
                    game.weaponB = game.weapons[selection];
                    if (game.weaponB == game.weaponA)
                        game.weaponA = new NullWeapon();
                    lag = 15;
                }
                else if (Master.controls.Start)
                {
                    game.Unpause();
                }
            }
        }
    }
}
