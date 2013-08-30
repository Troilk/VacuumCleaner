using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TomShane.Neoforce.Controls;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.GamerServices;

namespace VacuumCleaner
{
    public class VacuumCleanerMain : Application
    {

        #region Declarations

        public const int WINDOW_WIDTH = 1000;
        public const int WINDOW_HEIGHT = 600;

        #endregion

        #region Constructors

        public VacuumCleanerMain()
            : base(true)
        {
            Manager.SkinDirectory = @"..\..\..\..\..\Skins\";
            Graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            Graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            SystemBorder = false;
            FullScreenBorder = false;
            ClearBackground = false;
            Manager.TargetFrames = 60;
            ExitConfirmation = true;
        }

        protected override RenderTarget2D CreateRenderTarget()
        {
            return new RenderTarget2D(GraphicsDevice, Graphics.PreferredBackBufferWidth, Graphics.PreferredBackBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
        }

        #endregion

        #region Methods

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override Window CreateMainWindow()
        {
            return new MainWindow(Manager, this);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }


        #endregion

    }
}
