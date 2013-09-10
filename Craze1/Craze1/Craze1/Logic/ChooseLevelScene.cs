using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC_Test1.Logic;
using Cocos2D;

namespace Craze1.Logic
{
    class ChooseLevelScene : CCScene
    {
        public ChooseLevelScene()
        {
            AddChild(new ChooseLevelLayer());
        }
    }

    class ChooseLevelLayer : CCLayer
    {
        private CCMenuItem menuHeader;
        private CCMenuItemImage button8x10;
        private CCMenuItemImage button12x12;
        private CCMenuItemImage button13x15;

        public ChooseLevelLayer()
        {
            SetupBackground();
            menuHeader = new CCMenuItemImage("LevelsScene/game-mode.png", "LevelsScene/game-mode.png", OnGoBack);
            button8x10 = new CCMenuItemImage("LevelsScene/btn-8x10.png", "LevelsScene/btn-8x10-down.png", SetField8x10);
            button12x12 = new CCMenuItemImage("LevelsScene/btn-12x12.png", "LevelsScene/btn-12x12-down.png", SetField12x12);
            button13x15 = new CCMenuItemImage("LevelsScene/btn-13x15.png", "LevelsScene/btn-13x15-down.png", SetField13x15);
#if WINDOWS_PHONE||ANDROID
            button8x10.Scale = button12x12.Scale = button13x15.Scale = 0.6f;
#endif
            var chooseLevelMenu = new CCMenu(menuHeader, button8x10, button12x12, button13x15);
            chooseLevelMenu.AlignItemsVerticallyWithPadding(10);
            AddChild(chooseLevelMenu);


            menuHeader.Position += new CCPoint(0, 300);
            button13x15.Position -= new CCPoint(0, 1000);
        }

        private void SetupBackground()
        {
            string backgroundPath = "GameScene/background.png";
#if NETFX_CORE
            backgroundPath = @"GameScene/game_bg_win8.jpg";
#endif
            var background = new CCSprite(backgroundPath)
            {
                AnchorPoint = CCPoint.Zero,
                Position = CCPoint.Zero,
            };
            AddChild(background, -2);
        }

        private void SetField13x15(object obj)
        {
            Constants.KBoardWidth = 13;
            Constants.KBoardHeight = 15;
            NavigateToGameScene();
        }

        private void SetField12x12(object obj)
        {
            Constants.KBoardWidth = Constants.KBoardHeight = 12;
            NavigateToGameScene();
        }

        private void SetField8x10(object obj)
        {
            Constants.KBoardWidth = 8;
            Constants.KBoardHeight = 10;
            NavigateToGameScene();
        }

        private void OnGoBack(object obj)
        {
            CCDirector.SharedDirector.ReplaceScene(new MainScene());
        }

        private void NavigateToGameScene()
        {
            CCDirector.SharedDirector.ReplaceScene(new GameScene());
        }


        public override void OnEnter()
        {
            base.OnEnter();
            RunIntroAnimations();
        }

        private void RunIntroAnimations()
        {
            menuHeader.RunAction(new CCMoveBy(1, new CCPoint(0, -300)));
            button13x15.RunAction(new CCEaseBounceOut(new CCMoveBy(1f, new CCPoint(0, 1000))));
        }
    }
}
