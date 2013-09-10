using System.Collections.Generic;
using System.Linq;
using Cocos2D;
using CocosDenshion;
using Craze1.Logic;

namespace CC_Test1.Logic
{
    /// <summary>
    /// Экран меню. Состоит из одного слоя  <seealso cref="MainSceneLayer">MainSceneLayer</seealso>.
    /// </summary>
    public class MainScene : CCScene
    {
        public MainSceneLayer Layer = new MainSceneLayer();
        public MainScene()
        {
            AddChild(Layer);
        }
    }

    public class MainSceneLayer : CCLayer
    {
        public static int LastScore;
        private readonly Dictionary<CCSprite, float> _fallingGems = new Dictionary<CCSprite, float>();
        private MenuItem _selectedMenuItem;
        private CCSprite _background;
        private readonly CCSprite _logo;
        private readonly CCMenu _buttonsMenu;

        public MainSceneLayer()
        {
            SetupBackground();
            var starParticles = new CCParticleSystemQuad(@"Particles\bg-stars.plist");
            starParticles.SourcePosition = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, CCDirector.SharedDirector.WinSize.Height / 2);
            starParticles.PosVar = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, CCDirector.SharedDirector.WinSize.Height / 2);
            AddChild(starParticles);

            CCMenuItemImage buttonPlay = new CCMenuItemImage(@"MainScene\btn-play.png", @"MainScene\btn-play-down.png", OnPlayPressed);
            CCMenuItemImage buttonAbout = new CCMenuItemImage(@"MainScene\btn-about.png", @"MainScene\btn-about-down.png", OnAboutPressed);
            var buttonLevel = new CCMenuItemImage(@"MainScene\btn-levels.png", @"MainScene\btn-levels-down.png", OnChooseLevelPressed);

            var scale = 1f;
#if WINDOWS_PHONE||ANDROID
            scale = 0.6f;
#endif
            buttonAbout.Scale = buttonPlay.Scale = scale;
            buttonAbout.Opacity = buttonPlay.Opacity = 0;
            buttonLevel.Scale = scale;
            buttonLevel.Opacity = 0;


            _buttonsMenu = new CCMenu(buttonPlay, buttonLevel, buttonAbout);
            _buttonsMenu.AlignItemsVerticallyWithPadding(20);

            buttonAbout.Scale = buttonPlay.Scale = buttonLevel.Scale = 0.01f;

            _logo = new CCSprite(@"MainScene\logo.png");
            _logo.AnchorPoint = new CCPoint(0, 1);
            _logo.Scale = 0.05f;
            _logo.Opacity = 0;
            _logo.Position = new CCPoint(24, CCDirector.SharedDirector.WinSize.Height - 24);
            AddChild(_logo);
            AddChild(_buttonsMenu);


            CCSpriteFrameCache.SharedSpriteFrameCache.AddSpriteFramesWithFile("crystals.plist");
            Scheduler.ScheduleUpdateForTarget(this, 1, false);
            var scoreLabel = new CCLabelBMFont(LastScore.ToString(), @"Fonts\scorefont.fnt");
            scoreLabel.Position = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, 50);
            scoreLabel.AnchorPoint = new CCPoint(0.5f, 0f);

            var scoreLabelDescription = new CCLabelBMFont("ПОСЛЕДНИЙ СЧЕТ", @"Fonts\Foo64.fnt");
            scoreLabelDescription.Position = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, 55);
            scoreLabelDescription.AnchorPoint = new CCPoint(0.5f, 1f);
#if !ANDROID
            //CCSimpleAudioEngine.SharedEngine.PlayBackgroundMusic(@"Sounds\loop");
#endif

            AddChild(scoreLabelDescription);
            AddChild(scoreLabel);
        }

        private void SetupBackground()
        {
            string backgroundPath = "GameScene/background.png";
#if NETFX_CORE
            backgroundPath = @"GameScene/game_bg_win8.jpg";
#endif
            _background = new CCSprite(backgroundPath)
            {
                AnchorPoint = CCPoint.Zero,
                Position = CCPoint.Zero,
            };
            AddChild(_background, -2);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            RunIntroAnimations();
        }

        /// <summary>
        /// Нажатие на выбор уровня
        /// </summary>
        private void OnChooseLevelPressed(object obj)
        {
            _selectedMenuItem = MenuItem.Levels;
            RunOutroAnimations();
            CCSimpleAudioEngine.SharedEngine.PlayEffect(@"Sounds\click");
        }

        /// <summary>
        /// Нажатие на инфо
        /// </summary>
        private void OnAboutPressed(object obj)
        {
            _selectedMenuItem = MenuItem.About;
        }

        /// <summary>
        /// Нажатие на Жмяк
        /// </summary>
        private void OnPlayPressed(object obj)
        {
            _selectedMenuItem = MenuItem.Play;
            RunOutroAnimations();
            CCSimpleAudioEngine.SharedEngine.PlayEffect(@"Sounds\click");
        }

        private void RunIntroAnimations()
        {
            var scale = 1f;
#if WINDOWS_PHONE||ANDROID
            scale = 0.6f;
#endif
            var scaleAndFadeButtons = new CCSequence(new CCDelayTime(0.3f), new CCFadeIn(0.05f), new CCScaleTo(0.5f, scale));
            var scaleAndFadeLogo = new CCSequence(new CCDelayTime(0.3f), new CCFadeIn(0.05f), new CCScaleTo(0.5f, 0.5f));
            _logo.RunAction(scaleAndFadeLogo);
            foreach (var item in _buttonsMenu.Children)
            {
                var sequence = scaleAndFadeButtons.Copy();
                item.RunAction(sequence);
            }
        }


        /// <summary>
        /// Запускает анимацию перед переходом на другой экран
        /// </summary>
        private void RunOutroAnimations()
        {
            var scale = new CCScaleTo(0.5f, 0.05f);
            var fadeOut = new CCFadeOut(0.5f);

            _logo.RunAction(new CCSequence(scale, new CCCallFunc(NavigateToNextScene)));
            _logo.RunAction(fadeOut);
            foreach (var item in _buttonsMenu.Children)
            {
                var scaleCopy = scale.Copy();
                var fadeOutCopy = fadeOut.Copy();
                item.RunAction(scaleCopy);
                item.RunAction(fadeOutCopy);
            }
            var gems = _fallingGems.Keys.ToArray();
            for (int i = 0; i < gems.Length; i++)
            {
                var gem = gems[i];
                var fade = new CCFadeOut(0.5f);
                gem.RunAction(fade);
            }
        }

        /// <summary>
        /// Вызывается после завершения анимации. Переходит на другой экран
        /// </summary>
        private void NavigateToNextScene()
        {
            switch (_selectedMenuItem)
            {
                case MenuItem.Play:
                    CCDirector.SharedDirector.ReplaceScene(new GameScene());
                    break;
                case MenuItem.Levels:
                    CCDirector.SharedDirector.ReplaceScene(new ChooseLevelScene());
                    break;
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            UpdateGems();
        }

        /// <summary>
        /// Иговой цикл на экране с меню - падающие крисстальчики на фоне
        /// </summary>
        private void UpdateGems()
        {
            if (CCRandom.NextDouble() < 0.02)
            {

                var type = CCRandom.GetRandomInt(0, 4);
                var sprite = new CCSprite("crystalscrystals/" + type + ".png");
                var x = CCRandom.NextDouble() * CCDirector.SharedDirector.WinSize.Width + Constants.KGemSize / 2;
                var y = CCDirector.SharedDirector.WinSize.Height + Constants.KGemSize / 2;
                var scale = 0.2 + 0.8 * CCRandom.NextDouble();
                var speed = 2 * scale * Constants.KGemSize / 40;
                sprite.Position = new CCPoint((float)x, (float)y);
                sprite.Scale = (float)scale;
                _fallingGems.Add(sprite, (float)speed);
                _background.AddChild(sprite);
            }

            var gems = _fallingGems.Keys.ToArray();
            foreach (var gem in gems)
            {
                var speed = _fallingGems[gem];
                var pos = gem.Position;
                gem.Position = pos - new CCPoint(0, speed);
                if (pos.Y < -Constants.KGemSize / 2)
                {
                    _background.RemoveChild(gem, true);
                    _fallingGems.Remove(gem);
                }
            }
        }
    }

    enum MenuItem
    {
        Play,
        About,
        Levels
    }
}