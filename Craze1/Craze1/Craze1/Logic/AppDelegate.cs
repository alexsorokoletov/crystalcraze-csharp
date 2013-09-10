using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cocos2D;
using CocosDenshion;
using Microsoft.Xna.Framework;

namespace CC_Test1.Logic
{
    public partial class AppDelegate : CCApplication
    {
        private Vector2 _winSize;
        public AppDelegate(Game game, GraphicsDeviceManager graphics)
            : base(game, graphics)
        {
            s_pSharedApplication = this;
            _winSize = new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
#if NETFX_CORE
            CCDrawManager.InitializeDisplay(game, graphics, DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight);
#else
#if ANDROID
            CCDrawManager.InitializeDisplay(game, graphics, DisplayOrientation.Portrait | DisplayOrientation.PortraitDown);
#else
            CCDrawManager.InitializeDisplay(game, graphics, DisplayOrientation.Portrait);
#endif
#endif

            graphics.PreferMultiSampling = false;
        }

        /// <summary>
        ///  Implement CCDirector and CCScene init code here.
        /// </summary>
        /// <returns>
        ///  true  Initialize success, app continue.
        ///  false Initialize failed, app terminate.
        /// </returns>
        public override bool ApplicationDidFinishLaunching()
        {
            //initialize director
            CCDirector pDirector = CCDirector.SharedDirector;
            pDirector.SetOpenGlView();
#if NETFX_CORE
            CCDrawManager.SetDesignResolutionSize(_winSize.X, _winSize.Y, CCResolutionPolicy.ExactFit);
#else
#if ANDROID
            CCDrawManager.SetDesignResolutionSize(480, 800, CCResolutionPolicy.ExactFit);
#else
            CCDrawManager.SetDesignResolutionSize(480, 800, CCResolutionPolicy.ExactFit);
#endif
            //CCDrawManager.SetDesignResolutionSize(480, 320, ResolutionPolicy.ShowAll);
#endif
            // turn on display FPS
            pDirector.DisplayStats = true;

            // set FPS. the default value is 1.0/60 if you don't call this
            pDirector.AnimationInterval = 1.0 / 60;
            pDirector.RunWithScene(new MainScene());

            return true;
        }

        /// <summary>
        /// The function be called when the application enter background
        /// </summary>
        public override void ApplicationDidEnterBackground()
        {
            CCDirector.SharedDirector.Pause();
            CCSimpleAudioEngine.SharedEngine.PauseBackgroundMusic();
        }

        /// <summary>
        /// The function be called when the application enter foreground  
        /// </summary>
        public override void ApplicationWillEnterForeground()
        {
            CCDirector.SharedDirector.Resume();
            CCSimpleAudioEngine.SharedEngine.ResumeBackgroundMusic();
        }
    }

}
