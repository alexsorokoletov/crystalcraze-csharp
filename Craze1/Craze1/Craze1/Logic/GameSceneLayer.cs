using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cocos2D;
using CocosDenshion;

namespace CC_Test1.Logic
{
    /// <summary>
    /// Игровой экран. Состоит из одного слоя  <seealso cref="GameSceneLayer">GameSceneLayer</seealso>.
    /// </summary>
    public class GameScene : CCScene
    {
        public GameSceneLayer Layer = new GameSceneLayer();
        public GameScene()
        {
            AddChild(Layer);
        }
    }

    public class Constants
    {
        public const int KIntroTime = 1800;
        public static int KBoardWidth = 8;
        public static int KBoardHeight = 10;
        public static int KNumTotalGems
        {
            get
            {
                return KBoardWidth * KBoardHeight;
            }
        }
        public const int KTimeBetweenGemAdds = 8;
        public const int KTotalGameTime = 1000 * 20;
        public const int KNumRemovalFrames = 8;
        public const int KDelayBeforeHint = 3000;
        public const int KMaxTimeBetweenConsecutiveMoves = 1000;
        public const float KGameOverGemSpeed = 0.1f;
        public const float KGameOverGemAcceleration = 0.005f;
        public const int KBoardTypePup0 = 5;
        public const int KGemSize = 40;
    }

    public static class GameSettings
    {
        public static int LastScore = 0;
        public static int Level = 0;
    }

    public class GameSceneLayer : CCLayer
    {
        #region GameState
        private bool _isGameOver = false;
        private bool _isDisplayingHint = false;
        private DateTime _startTime;
        private DateTime _lastMoveTime;
        private bool _isPowerPlay = false;
        private bool _endTimerStarted = false;
        private int _numConsecutiveGems = 0;
        private int _score = 0;
        private bool _boardChangedSinceEvaluation;
        private int _possibleMove = -1;
        private int[] _board;
        private List<FallingGem>[] _fallingGems;
        private int[] _numGemsInColumn;
        private int[] _timeSinceAddInColumn;
        private CCSprite[] _boardSprites;
        private List<GameOverGem> _gameOverGems;
        #endregion

        #region Controls
        private CCProgressTimer _timer;
        private CCNode _particleLayer;
        private CCNode _gameBoardLayer;
        private CCNode _hintLayer;
        private CCNode _shimmerLayer;
        private CCNode _effectsLayer;
        private CCLabelBMFont _scoreLabel;
        private CCSprite _sprite;
        private CCParticleSystemQuad _powerPlayParticles;
        private CCLayerColor _powerPlayLayer;
        private CCPoint _fieldPositionZero = new CCPoint(0, 0);

        private CCNode _gameHeader;

        #endregion

        public GameSceneLayer()
        {
            CCSpriteFrameCache.SharedSpriteFrameCache.AddSpriteFramesWithFile("crystals.plist");
            SetupBackground();

            var boardSize = new CCPoint(Constants.KGemSize * Constants.KBoardWidth / 2f, Constants.KGemSize * Constants.KBoardHeight / 2f);
            var screenSize = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2f, CCDirector.SharedDirector.WinSize.Height / 2f);
            var timerScale = 1f;
#if WINDOWS_PHONE || ANDROID
            timerScale = 0.6f;
            _fieldPositionZero = (screenSize - boardSize);
#else
            _fieldPositionZero = (screenSize - boardSize);
#endif

            _gameHeader = new CCNode()
            {
                Position = new CCPoint(0, 200),
                AnchorPoint = new CCPoint(0, 0),
            };

            var header = new CCSprite(@"GameScene\header.png");
            header.ScaleX = CCDirector.SharedDirector.WinSize.Width / header.ContentSize.Width;
            header.Position = new CCPoint(0, CCDirector.SharedDirector.WinSize.Height);
            header.AnchorPoint = new CCPoint(0, 1);
            _gameHeader.AddChild(header);


            CCSprite timerBackground = new CCSprite(@"GameScene\timebar_bg.png") { Scale = timerScale };
            timerBackground.Position = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, CCDirector.SharedDirector.WinSize.Height - header.ContentSize.Height);
            timerBackground.AnchorPoint = new CCPoint(0.5f, 0f);
            _gameHeader.AddChild(timerBackground);

            _timer = new CCProgressTimer(@"GameScene\timebar.png") { Scale = timerScale };
            _timer.Position = timerBackground.Position;
            _timer.AnchorPoint = timerBackground.AnchorPoint;
            _timer.Midpoint = new CCPoint(0, 0.5f);
            _timer.Type = CCProgressTimerType.Bar;
            _timer.Percentage = 100;
            _timer.BarChangeRate = new CCPoint(1, 0);
            _gameHeader.AddChild(_timer);

            _scoreLabel = new CCLabelBMFont("0", @"Fonts\scorefont.fnt");
            _scoreLabel.Position = new CCPoint(CCDirector.SharedDirector.WinSize.Width - 24, CCDirector.SharedDirector.WinSize.Height - 24);
            _scoreLabel.AnchorPoint = new CCPoint(1f, 1f);
            _gameHeader.AddChild(_scoreLabel);

            AddChild(_gameHeader);

            _isGameOver = false;
            _isDisplayingHint = false;
            _startTime = DateTime.Now.AddMilliseconds(Constants.KIntroTime);
            _lastMoveTime = DateTime.Now;
            _numConsecutiveGems = 0;
            _isPowerPlay = false;
            _endTimerStarted = false;
            _score = 0;
            SetupBoard();

            _particleLayer = new CCParticleBatchNode("Particles/taken-gem.png", 250);
            _gameBoardLayer = new CCNode();
            _hintLayer = new CCNode();
            _shimmerLayer = new CCNode();
            _effectsLayer = new CCNode();

            AddChild(_shimmerLayer, -1);
            AddChild(_particleLayer, 1);
            AddChild(_gameBoardLayer, 0);
            AddChild(_hintLayer, 3);
            AddChild(_effectsLayer, 2);
            SetupShimmer();

            Scheduler.ScheduleUpdateForTarget(this, 1, false);
            TouchEnabled = true;
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

        public override void OnEnter()
        {
            base.OnEnter();
            RunIntroAnimations();
        }

        private void SetupBoard()
        {
            _board = new int[Constants.KNumTotalGems];
            for (var i = 0; i < Constants.KNumTotalGems; i++)
            {
                _board[i] = -1;
            }
            _boardSprites = new CCSprite[Constants.KNumTotalGems];
            _numGemsInColumn = new int[Constants.KBoardWidth];
            _timeSinceAddInColumn = new int[Constants.KBoardWidth];
            var x = 0;
            for (x = 0; x < Constants.KBoardWidth; x++)
            {
                _numGemsInColumn[x] = 0;
                _timeSinceAddInColumn[x] = 0;
            }
            _fallingGems = new List<FallingGem>[Constants.KBoardWidth];
            for (x = 0; x < Constants.KBoardWidth; x++)
            {
                _fallingGems[x] = new List<FallingGem>();
            }
            _boardChangedSinceEvaluation = true;
            _possibleMove = -1;
        }

        private void SetupShimmer()
        {
            CCSpriteFrameCache.SharedSpriteFrameCache.AddSpriteFramesWithFile("GameScene/shimmer.plist");
            for (var i = 0; i < 2; i++)
            {
                var sprt = new CCSprite("shimmergamescene/shimmer/bg-shimmer-" + i + ".png");

                CCActionInterval seqRot = null;
                CCActionInterval seqMov = null;
                CCActionInterval seqSca = null;

                float x;
                float y;
                float rot;

                for (var j = 0; j < 10; j++)
                {
                    var time = (float)(CCRandom.NextDouble() * 10 + 5);
                    x = Constants.KBoardWidth * Constants.KGemSize / 2;
                    y = (float)(CCRandom.NextDouble() * Constants.KBoardHeight * Constants.KGemSize);
                    rot = (float)(CCRandom.NextDouble() * 180 - 90);
                    var scale = (float)(CCRandom.NextDouble() * 3 + 3);

                    var actionRot = new CCEaseInOut(new CCRotateTo(time, rot), 2);
                    var actionMov = new CCEaseInOut(new CCMoveTo(time, new CCPoint(x, y)), 2);
                    var actionSca = new CCScaleTo(time, scale);

                    if (seqRot == null)
                    {
                        seqRot = actionRot;
                        seqMov = actionMov;
                        seqSca = actionSca;
                    }
                    else
                    {
                        seqRot = new CCSequence(seqRot, actionRot);
                        seqMov = new CCSequence(seqMov, actionMov);
                        seqSca = new CCSequence(seqSca, actionSca);
                    }
                }

                x = _fieldPositionZero.X + Constants.KBoardWidth * Constants.KGemSize / 2;
                y = _fieldPositionZero.Y + (float)CCRandom.NextDouble() * Constants.KBoardHeight * Constants.KGemSize;
                rot = (float)CCRandom.NextDouble() * 180 - 90;

                sprt.Position = new CCPoint(x, y);
                sprt.Rotation = rot;

                sprt.Position = new CCPoint(_fieldPositionZero.X + Constants.KBoardWidth * Constants.KGemSize / 2, _fieldPositionZero.Y + Constants.KBoardHeight * Constants.KGemSize / 2);
                sprt.BlendFunc = new CCBlendFunc(CCOGLES.GL_SRC_ALPHA, CCOGLES.GL_ONE);
                sprt.Scale = 3;

                _shimmerLayer.AddChild(sprt);
                sprt.Opacity = 0;
                sprt.RunAction(new CCRepeatForever(seqRot));
                sprt.RunAction(new CCRepeatForever(seqMov));
                sprt.RunAction(new CCRepeatForever(seqSca));

                sprt.RunAction(new CCFadeIn(2));
            }

        }

        public override void Update(float dt)
        {
            base.Update(dt);
            GameLoop();
        }

        private void GameLoop()
        {
            if (!_isGameOver)
            {
                RemoveMarkedGems();

                int x;
                FallingGem gem;

                // Add falling gems
                for (x = 0; x < Constants.KBoardWidth; x++)
                {
                    if (_numGemsInColumn[x] + _fallingGems[x].Count < Constants.KBoardHeight &&
                        _timeSinceAddInColumn[x] >= Constants.KTimeBetweenGemAdds)
                    {
                        // A gem should be added to this column!
                        var gemType = CCRandom.GetRandomInt(0, 4);
                        var gemSprite = new CCSprite("crystalscrystals/" + gemType + ".png");
                        gemSprite.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize, _fieldPositionZero.Y + Constants.KBoardHeight * Constants.KGemSize);
                        gemSprite.AnchorPoint = CCPoint.Zero;

                        gem = new FallingGem() { gemType = gemType, sprite = gemSprite, yPos = Constants.KBoardHeight, ySpeed = 0 };
                        _fallingGems[x].Add(gem);

                        _gameBoardLayer.AddChild(gemSprite);

                        _timeSinceAddInColumn[x] = 0;
                    }

                    _timeSinceAddInColumn[x]++;
                }

                #region Move falling gems
                var gemLanded = false;
                for (x = 0; x < Constants.KBoardWidth; x++)
                {
                    var column = _fallingGems[x];
                    var numFallingGems = _fallingGems[x].Count;
                    for (var i = numFallingGems - 1; i >= 0; i--)
                    {
                        gem = column[i];

                        gem.ySpeed += 0.06f;
                        gem.ySpeed *= 0.99f;
                        gem.yPos -= gem.ySpeed;

                        if (gem.yPos <= _numGemsInColumn[x])
                        {
                            // The gem hit the ground or a fixed gem
                            if (!gemLanded)
                            {
                                CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/tap-" + CCRandom.Next(0, 4) + ".wav");
                                gemLanded = true;
                            }

                            column.RemoveAt(i);

                            // Insert into board
                            var y = _numGemsInColumn[x];

                            if (_board[x + y * Constants.KBoardWidth] != -1)
                            {
                                Debug.WriteLine("Warning! Overwriting board idx: " + x + y * Constants.KBoardWidth + " type: " + _board[x + y * Constants.KBoardWidth]);
                            }

                            _board[x + y * Constants.KBoardWidth] = gem.gemType;
                            _boardSprites[x + y * Constants.KBoardWidth] = gem.sprite;

                            // Update fixed position
                            gem.sprite.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize, _fieldPositionZero.Y + y * Constants.KGemSize);
                            _numGemsInColumn[x]++;

                            _boardChangedSinceEvaluation = true;
                        }
                        else
                        {
                            // Update the falling gems position
                            gem.sprite.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize, _fieldPositionZero.Y + gem.yPos * Constants.KGemSize);
                        }
                    }
                }
                #endregion

                // Check if there are possible moves and no gems falling
                var isFallingGems = false;
                for (x = 0; x < Constants.KBoardWidth; x++)
                {
                    if (_numGemsInColumn[x] != Constants.KBoardHeight)
                    {
                        isFallingGems = true;
                        break;
                    }
                }

                if (!isFallingGems)
                {
                    var possibleMove = FindMove();
                    if (possibleMove == -1)
                    {
                        // Create a possible move
                        CreateRandomMove();
                    }
                }

                // Update timer
                var currentTime = DateTime.Now;
                var elapsedTime = (currentTime - _startTime).TotalMilliseconds / Constants.KTotalGameTime;
                var timeLeft = (1 - elapsedTime) * 100;
                if (timeLeft < 0) timeLeft = 0;
                if (timeLeft > 99.9) timeLeft = 99.9;

                _timer.Percentage = (float)timeLeft;

                // Update consecutive moves / powerplay
                if ((currentTime - _lastMoveTime).TotalMilliseconds > Constants.KMaxTimeBetweenConsecutiveMoves)
                {
                    _numConsecutiveGems = 0;
                }
                UpdatePowerPlay();

                // Update sparkles
                UpdateSparkles();

                // Check if timer sound should be played
                if (timeLeft < 6.6 && !_endTimerStarted)
                {
                    CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/timer.wav");
                    _endTimerStarted = true;
                }

                // Check for game over
                if (timeLeft < 0.000001f)
                {
                    CreateGameOver();
                    RunOutroAnimations();
                    _isGameOver = true;
                    CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/endgame.wav");
                    MainSceneLayer.LastScore = _score;
                }
                else if ((currentTime - _lastMoveTime).TotalMilliseconds > Constants.KDelayBeforeHint && !_isDisplayingHint)
                {
                    DisplayHint();
                }
            }
            else
            {
                UpdateGameOver();
            }
        }

        private void DisplayHint()
        {
            _isDisplayingHint = true;

            var idx = FindMove();
            var x = idx % Constants.KBoardWidth;
            var y = (int)Math.Floor((double)idx / Constants.KBoardWidth);

            var connected = FindConnectedGems(x, y);

            for (var i = 0; i < connected.Count; i++)
            {
                idx = connected[i];
                x = idx % Constants.KBoardWidth;
                y = (int)Math.Floor((double)idx / Constants.KBoardWidth);

                var actionFadeIn = new CCFadeIn(0.5f);
                var actionFadeOut = new CCFadeOut(0.5f);
                var actionSeq = new CCSequence(actionFadeIn, actionFadeOut);
                var action = new CCRepeatForever(actionSeq);

                var hintSprite = new CCSprite("crystalscrystals/hint.png");
                hintSprite.Opacity = 0;
                hintSprite.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize, _fieldPositionZero.Y + y * Constants.KGemSize);
                hintSprite.AnchorPoint = CCPoint.Zero;
                _hintLayer.AddChild(hintSprite);
                hintSprite.RunAction(action);
            }
        }

        private List<int> FindConnectedGems(int x, int y)
        {
            var connected = new List<int>();
            if (x + y * Constants.KBoardWidth < 0 || _board[x + y * Constants.KBoardWidth] <= -1) return connected;

            FindConnectedGemsRecursive(x, y, connected, _board[x + y * Constants.KBoardWidth]);

            return connected;
        }

        private void FindConnectedGemsRecursive(int x, int y, List<int> arr, int gemType)
        {
            // Check for bounds
            if (x < 0 || x >= Constants.KBoardWidth) return;
            if (y < 0 || y >= Constants.KBoardHeight) return;

            var idx = x + y * Constants.KBoardWidth;

            // Make sure that the gems type match
            if (_board[idx] != gemType) return;


            // Check if idx is already visited
            for (var i = 0; i < arr.Count; i++)
            {
                if (arr[i] == idx) return;
            }

            // Add idx to array
            arr.Add(idx);

            // Visit neighbours
            FindConnectedGemsRecursive(x + 1, y, arr, gemType);
            FindConnectedGemsRecursive(x - 1, y, arr, gemType);
            FindConnectedGemsRecursive(x, y + 1, arr, gemType);
            FindConnectedGemsRecursive(x, y - 1, arr, gemType);
        }

        private void RunOutroAnimations()
        {
            var slideOutHeader = new CCMoveTo(1, new CCPoint(0, 200));
            var sequence = new CCSequence(slideOutHeader, new CCDelayTime(0.2f), new CCCallFunc(() => CCDirector.SharedDirector.ReplaceScene(new MainScene())));
            _gameHeader.RunAction(sequence);
        }

        private void RunIntroAnimations()
        {
            var slideInHeader = new CCMoveTo(1, new CCPoint(0, 0));
            _gameHeader.RunAction(slideInHeader);
        }

        private void CreateGameOver()
        {
            _gameOverGems = new List<GameOverGem>();

            for (var x = 0; x < Constants.KBoardWidth; x++)
            {
                var column = _fallingGems[x];
                for (var i = 0; i < column.Count; i++)
                {
                    var gem = column[i];

                    var ySpeed = (float)(CCRandom.NextDouble() * 2 - 1) * Constants.KGameOverGemSpeed;
                    var xSpeed = (float)(CCRandom.NextDouble() * 2 - 1) * Constants.KGameOverGemSpeed;

                    var gameOverGem = new GameOverGem() { sprite = gem.sprite, xPos = x, yPos = gem.yPos, ySpeed = ySpeed, xSpeed = xSpeed };
                    _gameOverGems.Add(gameOverGem);
                }

                for (var y = 0; y < Constants.KBoardHeight; y++)
                {
                    var i1 = x + y * Constants.KBoardWidth;
                    if (_boardSprites[i1] != null)
                    {
                        var ySpeed1 = (float)(CCRandom.NextDouble() * 2 - 1) * Constants.KGameOverGemSpeed;
                        var xSpeed1 = (float)(CCRandom.NextDouble() * 2 - 1) * Constants.KGameOverGemSpeed;
                        var gameOverGem1 = new GameOverGem() { sprite = _boardSprites[i1], xPos = x, yPos = y, ySpeed = ySpeed1, xSpeed = xSpeed1 };
                        _gameOverGems.Add(gameOverGem1);
                    }
                }
            }

            _hintLayer.RemoveAllChildrenWithCleanup(true);

            RemoveShimmer();
        }

        private void RemoveShimmer()
        {
            var children = _shimmerLayer.Children;
            for (var i = 0; i < children.Count; i++)
            {
                children[i].RunAction(new CCFadeOut(1));
            }
        }

        private void UpdateSparkles()
        {
            if (CCRandom.NextDouble() > 0.1) return;
            var idx = CCRandom.Next(Constants.KNumTotalGems);
            var gemSprite = _boardSprites[idx];
            if (_board[idx] < 0 || _board[idx] >= 5) return;
            if (gemSprite == null) return;

            if (gemSprite.ChildrenCount > 0) return;

            _sprite = new CCSprite("crystalscrystals/sparkle.png");
            _sprite.RunAction(new CCRepeatForever(new CCRotateBy(3, 360)));

            _sprite.Opacity = 0;

            _sprite.RunAction(new CCSequence(
                new CCFadeIn(0.5f),
                new CCFadeOut(2),
                new CCRemoveSelf(true)));

            _sprite.Position = new CCPoint(Constants.KGemSize * (2f / 6), Constants.KGemSize * (4f / 6));

            gemSprite.AddChild(_sprite);
        }

        private void UpdatePowerPlay()
        {
            var powerPlay = (_numConsecutiveGems >= 5);
            if (powerPlay == _isPowerPlay) return;

            if (powerPlay)
            {
                // Start power-play
                _powerPlayParticles = new CCParticleSystemQuad("Particles/power-play.plist");
                _powerPlayParticles.AutoRemoveOnFinish = (true);
                _powerPlayParticles.SourcePosition = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, CCDirector.SharedDirector.WinSize.Height / 2);
                _powerPlayParticles.PosVar = new CCPoint(CCDirector.SharedDirector.WinSize.Width / 2, CCDirector.SharedDirector.WinSize.Height / 2);
                _particleLayer.AddChild(_powerPlayParticles);


                var contentSize = CCDirector.SharedDirector.WinSize;
                _powerPlayLayer = new CCLayerColor(new CCColor4B(85, 0, 70, 0), contentSize.Width, contentSize.Height);

                var action = new CCSequence(new CCFadeIn(0.25f), new CCFadeOut(0.25f));
                _powerPlayLayer.RunAction(new CCRepeatForever(action));
                _powerPlayLayer.BlendFunc = CCBlendFunc.Additive;

                _effectsLayer.AddChild(_powerPlayLayer);

            }
            else
            {
                // Stop power-play
                if (_powerPlayParticles != null)
                {
                    _powerPlayParticles.StopSystem();

                    _powerPlayLayer.StopAllActions();
                    _powerPlayLayer.RunAction(new CCSequence(new CCFadeOut(0.5f), new CCRemoveSelf(true)));
                }
            }

            _isPowerPlay = powerPlay;
        }

        private void CreateRandomMove()
        {
            // Find a random place in the lower part of the board
            var x = CCRandom.Next(Constants.KBoardWidth);
            var y = CCRandom.Next(Constants.KBoardHeight / 2);

            // Make sure it is a gem that we found
            var gemType = _board[x + y * Constants.KBoardWidth];
            if (gemType == -1 || gemType >= 5) return;

            // Change the color of two surrounding gems
            SetGemType(x + 1, y, gemType);
            SetGemType(x, y + 1, gemType);

            _boardChangedSinceEvaluation = true;
        }

        private void SetGemType(int x, int y, int newType)
        {
            // Check bounds
            if (x < 0 || x >= Constants.KBoardWidth) return;
            if (y < 0 || y >= Constants.KBoardHeight) return;

            // Get the type of the gem
            var idx = x + y * Constants.KBoardWidth;
            var gemType = _board[idx];

            // Make sure that it is a gem
            if (gemType < 0 || gemType >= 5) return;

            _board[idx] = newType;

            // Remove old gem and insert a new one
            _gameBoardLayer.RemoveChild(_boardSprites[idx], true);

            var gemSprite = new CCSprite("crystalscrystals/" + newType + ".png");
            gemSprite.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize, _fieldPositionZero.Y + y * Constants.KGemSize);
            gemSprite.AnchorPoint = CCPoint.Zero;

            _gameBoardLayer.AddChild(gemSprite);
            _boardSprites[idx] = gemSprite;

            _boardChangedSinceEvaluation = true;
        }

        private int GetGemType(int x, int y)
        {
            if (x < 0 || x >= Constants.KBoardWidth) return -1;
            if (y < 0 || y >= Constants.KBoardHeight) return -1;

            return _board[x + y * Constants.KBoardWidth];
        }

        private int FindMove()
        {
            if (!_boardChangedSinceEvaluation)
            {
                return _possibleMove;
            }

            // Iterate through all places on the board
            for (var y = 0; y < Constants.KBoardHeight; y++)
            {
                for (var x = 0; x < Constants.KBoardWidth; x++)
                {
                    var idx = x + y * Constants.KBoardWidth;
                    var gemType = _board[idx];

                    // Make sure that it is a gem
                    if (gemType < 0 || gemType >= 5) continue;

                    // Check surrounding tiles
                    var numSimilar = 0;

                    if (GetGemType(x - 1, y) == gemType) numSimilar++;
                    if (GetGemType(x + 1, y) == gemType) numSimilar++;
                    if (GetGemType(x, y - 1) == gemType) numSimilar++;
                    if (GetGemType(x, y + 1) == gemType) numSimilar++;

                    if (numSimilar >= 2)
                    {
                        _possibleMove = idx;
                        return idx;
                    }
                }
            }
            _boardChangedSinceEvaluation = false;
            _possibleMove = -1;
            return -1;
        }

        private void RemoveMarkedGems()
        {
            // Iterate through the board
            for (var x = 0; x < Constants.KBoardWidth; x++)
            {
                for (var y = 0; y < Constants.KBoardHeight; y++)
                {
                    var i = x + y * Constants.KBoardWidth;

                    if (_board[i] < -1)
                    {
                        // Increase the count for negative crystal types
                        _board[i]++;
                        if (_board[i] == -1)
                        {
                            _numGemsInColumn[x]--;
                            _boardChangedSinceEvaluation = true;

                            // Transform any gem above this to a falling gem
                            for (var yAbove = y + 1; yAbove < Constants.KBoardHeight; yAbove++)
                            {
                                var idxAbove = x + yAbove * Constants.KBoardWidth;

                                if (_board[idxAbove] < -1)
                                {
                                    _numGemsInColumn[x]--;
                                    _board[idxAbove] = -1;
                                }
                                if (_board[idxAbove] == -1) continue;

                                // The gem is not connected, make it into a falling gem
                                var gemType = _board[idxAbove];
                                var gemSprite = _boardSprites[idxAbove];

                                var gem = new FallingGem() { gemType = gemType, sprite = gemSprite, yPos = yAbove, ySpeed = 0 };
                                _fallingGems[x].Add(gem);

                                // Remove from board
                                _board[idxAbove] = -1;
                                _boardSprites[idxAbove] = null;

                                _numGemsInColumn[x]--;
                            }

                        }
                    }
                }
            }
        }

        private void UpdateGameOver()
        {
            for (var i = 0; i < _gameOverGems.Count; i++)
            {
                var gem = _gameOverGems[i];

                gem.xPos = gem.xPos + gem.xSpeed;
                gem.yPos = gem.yPos + gem.ySpeed;
                gem.ySpeed -= Constants.KGameOverGemAcceleration;

                gem.sprite.Position = new CCPoint(_fieldPositionZero.X + gem.xPos * Constants.KGemSize, _fieldPositionZero.Y + gem.yPos * Constants.KGemSize);
            }
        }

        public override void TouchesBegan(List<CCTouch> touches)
        {
            var location = touches[0];
            ProcessClick(location);
            base.TouchesBegan(touches);
        }

        public override bool TouchBegan(CCTouch touch)
        {
            ProcessClick(touch);
            return base.TouchBegan(touch);
        }

        private void ProcessClick(CCTouch touch)
        {
            var loc = touch.Location - _gameBoardLayer.Position;
            loc = loc - _fieldPositionZero;

            var x = (int)Math.Floor(loc.X / Constants.KGemSize);
            var y = (int)Math.Floor(loc.Y / Constants.KGemSize);

            if (!_isGameOver)
            {
                _hintLayer.RemoveAllChildrenWithCleanup(true);
                _isDisplayingHint = false;

                if (ActivatePowerUp(x, y) ||
                    RemoveConnectedGems(x, y))
                {
                    // Player did a valid move
                    var sound = _numConsecutiveGems;
                    if (sound > 4) sound = 4;
                    CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/gem-" + sound + ".wav");

                    _numConsecutiveGems++;
                }
                else
                {
                    _numConsecutiveGems = 0;
                }

                _lastMoveTime = DateTime.Now;
            }
        }

        private bool RemoveConnectedGems(int x, int y)
        {
            // Check for bounds
            if (x < 0 || x >= Constants.KBoardWidth) return false;
            if (y < 0 || y >= Constants.KBoardHeight) return false;

            var connected = FindConnectedGems(x, y);
            var removedGems = false;

            if (connected.Count >= 3)
            {
                _boardChangedSinceEvaluation = true;
                removedGems = true;

                AddScore(100 * connected.Count);

                var idxPup = -1;
                int pupX = 0;
                int pupY = 0;
                if (connected.Count >= 6)
                {
                    // Add power-up
                    idxPup = connected[(int)Math.Floor(CCRandom.NextDouble() * connected.Count)];
                    pupX = idxPup % Constants.KBoardWidth;
                    pupY = (int)Math.Floor((double)idxPup / Constants.KBoardWidth);
                }

                for (var i = 0; i < connected.Count; i++)
                {
                    var idx = connected[i];
                    var gemX = idx % Constants.KBoardWidth;
                    var gemY = (int)Math.Floor((double)idx / Constants.KBoardWidth);

                    _board[idx] = -Constants.KNumRemovalFrames;
                    _gameBoardLayer.RemoveChild(_boardSprites[idx], true);
                    _boardSprites[idx] = null;

                    // Add particle effect
                    var particle = new CCParticleSystemQuad("Particles/taken-gem.plist");
                    particle.Position = new CCPoint(_fieldPositionZero.X + gemX * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + gemY * Constants.KGemSize + Constants.KGemSize / 2);
                    particle.AutoRemoveOnFinish = (true);
                    _particleLayer.AddChild(particle);

                    // Add power-up
                    if (idx == idxPup)
                    {
                        _board[idx] = Constants.KBoardTypePup0;

                        var sprt = new CCSprite("crystalscrystals/bomb.png");
                        sprt.Position = new CCPoint(_fieldPositionZero.X + gemX * Constants.KGemSize, _fieldPositionZero.Y + gemY * Constants.KGemSize);
                        sprt.AnchorPoint = CCPoint.Zero;
                        sprt.Opacity = (0);
                        sprt.RunAction(new CCFadeIn(0.4f));

                        var sprtGlow = new CCSprite("crystalscrystals/bomb-hi.png");
                        sprtGlow.AnchorPoint = CCPoint.Zero;
                        sprtGlow.Opacity = (0);
                        sprtGlow.RunAction(new CCRepeatForever(new CCSequence(new CCFadeIn(0.4f), new CCFadeOut(0.4f))));
                        sprt.AddChild(sprtGlow);

                        _boardSprites[idx] = sprt;
                        _gameBoardLayer.AddChild(sprt);
                    }
                    else if (idxPup != -1)
                    {
                        // Animate effect for power-up
                        var sprtLight = new CCSprite("crystalscrystals/bomb-light.png");
                        sprtLight.Position = new CCPoint(_fieldPositionZero.X + gemX * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + gemY * Constants.KGemSize + Constants.KGemSize / 2);
                        sprtLight.BlendFunc = CCBlendFunc.Additive;
                        _effectsLayer.AddChild(sprtLight);

                        var movAction = new CCMoveTo(0.2f, new CCPoint(_fieldPositionZero.X + pupX * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + pupY * Constants.KGemSize + Constants.KGemSize / 2));
                        var seqAction = new CCSequence(movAction, new CCRemoveSelf());

                        sprtLight.RunAction(seqAction);
                    }
                }
            }
            else
            {
                CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/miss.wav");
            }

            _lastMoveTime = DateTime.Now;

            return removedGems;
        }

        private void AddScore(int score)
        {
            if (_isPowerPlay) score *= 3;
            _score += score;
            _scoreLabel.Text = _score.ToString();
        }

        private bool ActivatePowerUp(int x, int y)
        {
            // Check for bounds
            if (x < 0 || x >= Constants.KBoardWidth) return false;
            if (y < 0 || y >= Constants.KBoardHeight) return false;

            var removedGems = false;

            var idx = x + y * Constants.KBoardWidth;
            if (_board[idx] == Constants.KBoardTypePup0)
            {
                // Activate bomb
                CCSimpleAudioEngine.SharedEngine.PlayEffect("Sounds/powerup.wav");

                removedGems = true;

                AddScore(2000);

                _board[idx] = -Constants.KNumRemovalFrames;
                _gameBoardLayer.RemoveChild(_boardSprites[idx], true);
                _boardSprites[idx] = null;

                // Remove a horizontal line
                int idxRemove;
                for (var xRemove = 0; xRemove < Constants.KBoardWidth; xRemove++)
                {
                    idxRemove = xRemove + y * Constants.KBoardWidth;
                    if (_board[idxRemove] >= 0 && _board[idxRemove] < 5)
                    {
                        _board[idxRemove] = -Constants.KNumRemovalFrames;
                        _gameBoardLayer.RemoveChild(_boardSprites[idxRemove], true);
                        _boardSprites[idxRemove] = null;
                    }
                }

                // Remove a vertical line
                for (var yRemove = 0; yRemove < Constants.KBoardHeight; yRemove++)
                {
                    idxRemove = x + yRemove * Constants.KBoardWidth;
                    if (_board[idxRemove] >= 0 && _board[idxRemove] < 5)
                    {
                        _board[idxRemove] = -Constants.KNumRemovalFrames;
                        _gameBoardLayer.RemoveChild(_boardSprites[idxRemove], true);
                        _boardSprites[idxRemove] = null;
                    }
                }

                // Add particle effects
                var hp = new CCParticleSystemQuad("Particles/taken-hrow.plist");
                hp.Position = new CCPoint(_fieldPositionZero.X + Constants.KBoardWidth / 2 * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + y * Constants.KGemSize + Constants.KGemSize / 2);
                hp.AutoRemoveOnFinish = (true);
                _particleLayer.AddChild(hp);

                var vp = new CCParticleSystemQuad("Particles/taken-vrow.plist");
                vp.Position = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + Constants.KBoardHeight / 2 * Constants.KGemSize + Constants.KGemSize / 2);
                vp.AutoRemoveOnFinish = (true);
                _particleLayer.AddChild(vp);

                // Add explo anim
                var center = new CCPoint(_fieldPositionZero.X + x * Constants.KGemSize + Constants.KGemSize / 2, _fieldPositionZero.Y + y * Constants.KGemSize + Constants.KGemSize / 2);

                // Horizontal
                var sprtH0 = new CCSprite("crystalscrystals/bomb-explo.png");
                sprtH0.BlendFunc = new CCBlendFunc(CCOGLES.GL_SRC_ALPHA, CCOGLES.GL_ONE);
                sprtH0.Position = (center);
                sprtH0.ScaleX = (5);
                sprtH0.RunAction(new CCScaleTo(0.5f, 30, 1));
                sprtH0.RunAction(new CCSequence(new CCFadeOut(0.5f), new CCRemoveSelf(true)));
                _effectsLayer.AddChild(sprtH0);

                // Vertical
                var sprtV0 = new CCSprite("crystalscrystals/bomb-explo.png");
                sprtV0.BlendFunc = new CCBlendFunc(CCOGLES.GL_SRC_ALPHA, CCOGLES.GL_ONE);
                sprtV0.Position = (center);
                sprtV0.ScaleY = (5);
                sprtV0.RunAction(new CCScaleTo(0.5f, 1, 30));
                sprtV0.RunAction(new CCSequence(new CCFadeOut(0.5f), new CCRemoveSelf(true)));
                _effectsLayer.AddChild(sprtV0);

                // Horizontal
                var sprtH1 = new CCSprite("crystalscrystals/bomb-explo-inner.png");
                sprtH1.BlendFunc = new CCBlendFunc(CCOGLES.GL_SRC_ALPHA, CCOGLES.GL_ONE);
                sprtH1.Position = (center);
                sprtH1.ScaleX = (0.5f);
                sprtH1.RunAction(new CCScaleTo(0.5f, 8, 1));
                sprtH1.RunAction(new CCSequence(new CCFadeOut(0.5f), new CCRemoveSelf(true)));
                _effectsLayer.AddChild(sprtH1);

                // Vertical
                var sprtV1 = new CCSprite("crystalscrystals/bomb-explo-inner.png");
                sprtV1.Rotation = (90);
                sprtV1.BlendFunc = new CCBlendFunc(CCOGLES.GL_SRC_ALPHA, CCOGLES.GL_ONE);
                sprtV1.Position = (center);
                sprtV1.ScaleY = (0.5f);
                sprtV1.RunAction(new CCScaleTo(0.5f, 8, 1));
                sprtV1.RunAction(new CCSequence(new CCFadeOut(0.5f), new CCRemoveSelf(true)));
                _effectsLayer.AddChild(sprtV1);
            }

            return removedGems;
        }
    }
}